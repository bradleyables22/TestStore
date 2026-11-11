using Heimdall.Server;
using Heimdall.Server.Rendering;
using Microsoft.AspNetCore.Html;
using TestStore.Shared;

namespace TestStore.Catalog.Demo;

public sealed class CatalogInvocations(PreviewOptions previews)
{
    [ContentInvocation("catalog.load")]
    public IHtmlContent Load([ContentPayload] StoreRequest request)
    {
        request.Validate();
        return FluentHtml.Section(section =>
        {
            section.Id(StoreView.RegionId).Class("catalog")
                .Div(hero => hero.Class("hero").Span(s => s.Class("eyebrow").Text("THE EVERYDAY COLLECTION"))
                    .H1(h => h.Text("Little things.\nGood company."))
                    .P(p => p.Text("Four useful things for your desk, your bag, and the days in between.")))
                .Div(grid =>
                {
                    grid.Class("product-grid");
                    foreach (var product in Products.All)
                    {
                        var payload = new StoreRequest { CartId = request.CartId, CartPath = request.CartPath, ProductId = product.Id };
                        grid.Tag("article", card => card.Class("product-card")
                            .Div(art => art.Class("product-art " + product.Id).Attr("style", "--product-color:" + product.Color)
                                .Span(s => s.Class("product-object").Attr("aria-hidden", "true").Text(product.Id == "notebook" ? "NOTES" : "")))
                            .Div(info => info.Class("product-info").Span(s => s.Class("eyebrow").Text(product.Category))
                                .Div(row => row.Class("product-heading").H2(h => h.Text(product.Name)).Span(s => s.Text(StoreView.Money(product.Price))))
                                .P(p => p.Text(product.Description))
                                .Add(StoreView.Button("Add to bag", "catalog.choose", payload))));
                    }
                });
            if (previews.Enabled && string.IsNullOrEmpty(request.CartPath) && Products.Find(request.ProductId) is { } selected)
                section.P(p => p.Attr("role", "status").Text("Preview selected: " + selected.Name + ". Open AppHost to try the complete cart handoff."));
            section.Add(StoreView.Diagnostics("Catalog"));
        });
    }

    [ContentInvocation("catalog.choose")]
    public IHtmlContent Choose([ContentPayload] StoreRequest request)
    {
        request.Validate();
        if (Products.Find(request.ProductId) is null) return StoreView.Error("That product does not exist.");
        if (previews.Enabled && string.IsNullOrEmpty(request.CartPath)) return Load(request);
        // Navigation switches the document to the Cart backend. The Cart load POST
        // performs the add exactly once for this operation ID; GET never changes a cart.
        // Only shell-relative route paths are accepted; never redirect to a supplied origin.
        if (!System.Text.RegularExpressions.Regex.IsMatch(request.CartPath, @"^/(?:[A-Za-z0-9_-]+/?)*$"))
            return StoreView.Error("The shell must supply a local cart route.");
        var handoff = new StoreRequest { CartId = request.CartId, ProductId = request.ProductId, OperationId = Guid.NewGuid().ToString("N") };
        return FluentHtml.Fragment(fragment => fragment.Add(
            HeimdallHtml.SetStorage(HeimdallHtml.StorageArea.Session, StoreHandoff.Key,
                System.Text.Json.JsonSerializer.Serialize(handoff, new System.Text.Json.JsonSerializerOptions(System.Text.Json.JsonSerializerDefaults.Web))),
            HeimdallHtml.Abort("cart-handoff"),
            // Redirect directives short-circuit storage processing. Navigate after commands instead.
            HeimdallHtml.JsInvokeVoidAfter("window.Store.openCart", request.CartPath, handoff.OperationId)));
    }
}
