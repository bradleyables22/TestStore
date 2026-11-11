using Heimdall.Server.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace TestStore.Shared;

public sealed record PreviewOptions(bool Enabled);

public static class PreviewPage
{
    public static IHtmlContent Render(HttpContext context, string backend)
    {
        context.Response.Headers.CacheControl = "no-store";
        var cookieName = "TestStore.Preview." + backend;
        var id = context.Request.Cookies[cookieName];
        if (!Guid.TryParseExact(id, "N", out _)) id = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append(cookieName, id!, new CookieOptions
        { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromDays(1) });
        var request = new StoreRequest { CartId = id! };
        if (backend == "Cart")
        {
            // Seed once per preview cart through its POST, never during the page GET.
            request.ProductId = "mug";
            request.OperationId = id;
        }
        return FluentHtml.Fragment(f => f.Raw("<!doctype html>").HtmlTag(html => html.Attr("lang", "en")
            .Head(head => head.Meta(m => m.Attr("charset", "utf-8"))
                .Meta(m => m.Name("viewport").ContentAttr("width=device-width,initial-scale=1"))
                .Title(t => t.Text(backend + " preview"))
                .Link(l => l.Attr("rel", "stylesheet").Href("/assets/store.css"))
                .Script(s => s.Src("/_content/HeimdallFramework.Web/heimdall-bundle.min.js")))
            .Body(body => body.Main(main => main.Class("page")
                .P(p => p.Class("eyebrow").Text(backend + " preview · independent backend"))
                .Section(section => section.Id(StoreView.RegionId).Class("loading")
                    .Text("Loading preview…")
                    .Add(HeimdallHtml.OnLoad(backend.ToLowerInvariant() + ".load"),
                        HeimdallHtml.Payload(request), HeimdallHtml.Target("#" + StoreView.RegionId),
                        HeimdallHtml.SwapMode(HeimdallHtml.Swap.Outer)))))));
    }
}
