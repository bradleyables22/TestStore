using Heimdall.Server;
using Heimdall.Server.Rendering;
using Microsoft.AspNetCore.Html;
using TestStore.Shared;

namespace TestStore.Cart.Demo;

public sealed class CartInvocations(CartStore store)
{
    [ContentInvocation("cart.load")]
    public async Task<IHtmlContent> Load([ContentPayload] StoreRequest request, CancellationToken ct)
    {
        request.Validate();
        var cart = await store.UpdateAsync(request.CartId, cart =>
        {
            if (request.ProductId is null or "" || request.OperationId is null or "") return;
            if (Products.Find(request.ProductId) is null) throw new ArgumentException("Unknown product.");
            CartStore.Once(cart, request.OperationId, () =>
            {
                cart.Items[request.ProductId] = Math.Min(10, cart.Items.GetValueOrDefault(request.ProductId) + 1);
                cart.Receipt = null;
            });
        }, ct);
        // Only acknowledge/clear the handoff after the persistence operation succeeds.
        return FluentHtml.Fragment(fragment => fragment.Add(
            HeimdallHtml.RemoveStorage(HeimdallHtml.StorageArea.Session, StoreHandoff.Key),
            Render(request, cart)));
    }

    [ContentInvocation("cart.quantity")]
    public async Task<IHtmlContent> Quantity([ContentPayload] StoreRequest request, CancellationToken ct)
    {
        request.Validate();
        if (Products.Find(request.ProductId) is null || request.Quantity is < 0 or > 10)
            return StoreView.Error("Choose a valid product and a quantity between 0 and 10.");
        var cart = await store.UpdateAsync(request.CartId, cart => CartStore.Once(cart, request.OperationId, () =>
        {
            if (request.Quantity == 0) cart.Items.Remove(request.ProductId!);
            else cart.Items[request.ProductId!] = request.Quantity;
            cart.Receipt = null;
        }), ct);
        return Render(request, cart);
    }

    [ContentInvocation("cart.checkout")]
    public async Task<IHtmlContent> Checkout([ContentPayload] StoreRequest request, CancellationToken ct)
    {
        request.Validate();
        var cart = await store.UpdateAsync(request.CartId, cart => CartStore.Once(cart, request.OperationId, () =>
        {
            if (cart.Items.Count == 0) return;
            cart.Receipt = "DEMO-" + request.OperationId![..8].ToUpperInvariant();
            cart.Items.Clear();
        }), ct);
        return Render(request, cart);
    }

    private IHtmlContent Render(StoreRequest request, CartDocument cart) => FluentHtml.Section(section =>
    {
        section.Id(StoreView.RegionId).Class("cart")
            .Span(s => s.Class("eyebrow").Text("THE GOOD STUFF"))
            .H1(h => h.Text(cart.Receipt is null ? "Your bag." : "All set."));
        if (cart.Receipt is not null)
            section.Div(d => d.Class("receipt").H2(h => h.Text("A pretend order. A real round trip."))
                .P(p => p.Text("Confirmation " + cart.Receipt + ". No payment was taken.")));
        if (cart.Items.Count == 0)
            section.Div(d => d.Class("empty").P(p => p.Text(cart.Receipt is null ? "Your bag is waiting for something good." : "Thanks for trying the store.")));
        else
        {
            decimal total = 0;
            foreach (var (id, quantity) in cart.Items)
            {
                var product = Products.Find(id)!;
                total += product.Price * quantity;
                StoreRequest Change(int value) => new() { CartId = request.CartId, ProductId = id, Quantity = value, OperationId = Guid.NewGuid().ToString("N") };
                section.Tag("article", row => row.Class("cart-row").Attr("data-product", id)
                    .Div(d => d.Class("cart-swatch").Attr("style", "background:" + product.Color).Text(product.Name[..1]))
                    .Div(d => d.Class("cart-description").H2(h => h.Text(product.Name)).P(p => p.Text(StoreView.Money(product.Price) + " each")))
                    .Div(d => d.Class("quantity").Add(StoreView.Button("−", "cart.quantity", Change(quantity - 1), "quantity-button"))
                        .Span(s => s.Attr("data-quantity", quantity.ToString()).Text(quantity.ToString()))
                        .Add(StoreView.Button("+", "cart.quantity", Change(Math.Min(10, quantity + 1)), "quantity-button")))
                    .Strong(s => s.Text(StoreView.Money(product.Price * quantity)))
                    .Add(StoreView.Button("Remove", "cart.quantity", Change(0), "text-button")));
            }
            section.Div(d => d.Class("cart-total").Span(s => s.Text("Total")).Strong(s => s.Text(StoreView.Money(total))))
                .Div(d => d.Class("cart-checkout").P(p => p.Text("Demo checkout only. No account, address, or payment needed."))
                    .Add(StoreView.Button("Place demo order", "cart.checkout", new StoreRequest { CartId = request.CartId, OperationId = Guid.NewGuid().ToString("N") })));
        }
        section.Add(StoreView.Diagnostics("Cart"));
    });
}
