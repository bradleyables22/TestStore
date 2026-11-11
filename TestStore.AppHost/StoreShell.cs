using System.Text.Json;
using Heimdall.Server.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;
namespace TestStore.Shared;
public static class StoreShell
{
    public static IHtmlContent Page(HttpContext context, StoreOptions options, StoreRequest request,
        string backendOrigin, string title, string loadInvocation)
        => FluentHtml.Fragment(f => f.Raw("<!doctype html>").HtmlTag(html => html.Attr("lang", "en")
            .Head(head => head.Meta(m => m.Attr("charset", "utf-8"))
                .Meta(m => m.Name("viewport").ContentAttr("width=device-width,initial-scale=1"))
                .Title(t => t.Text(title + " · Small Goods"))
                .Link(l => l.Attr("rel", "stylesheet").Href("/assets/store.css"))
                .Script(s => s.Src("/_content/HeimdallFramework.Web/heimdall-bundle.min.js"))
                .Script(s => s.Raw("const storeBackend=" + JsonSerializer.Serialize(backendOrigin) + ";" + "Object.assign(Heimdall.config.endpoints,{contentInvocations:storeBackend+'/__heimdall/v1/content/invocations',csrf:storeBackend+'/__heimdall/v1/csrf'});Heimdall.config.credentials='include';"))
                .Script(s => s.Src("/assets/store.js")))
            .Body(body => body
                .Tag("header", h => h.Class("site-header").A(a => a.Class("brand").Href("/").Text("small goods")))
                .Tag("nav", nav => nav.Attr("aria-label", "Main navigation").Class("site-nav")
                    .A(a => a.Href("/").Text("Shop"))
                    .A(a => a.Href("/cart").Text("Your bag"))
                    .Span(s => s.Class("mode-pill").Text("APPHOST SHELL")))
                .Main(main => main.Id("main").Class("page")
                    .Div(d => d.Id("request-error").Class("request-error").Attr("role", "alert").Attr("hidden", "hidden"))
                    .Section(section => section.Id(StoreView.RegionId).Class("loading").Attr("aria-live", "polite")
                        .H1(h => h.Text("Loading " + title.ToLowerInvariant() + "…"))
                        .P(p => p.Text("Connecting to the " + title.ToLowerInvariant() + " backend."))
                        .Add(HeimdallHtml.OnLoad(loadInvocation), HeimdallHtml.Payload(request), HeimdallHtml.Target("#" + StoreView.RegionId), HeimdallHtml.SwapMode(HeimdallHtml.Swap.Outer))))
                .Script(s => { if (loadInvocation == "cart.load") s.Raw("window.Store.prepareCartLoad(" + JsonSerializer.Serialize(request, new JsonSerializerOptions(JsonSerializerDefaults.Web)) + ");"); })
                .Tag("footer", foot => foot.Class("site-footer").Span(s => s.Text("A tiny distributed store. Nothing here is for sale."))))));
}
