using System.Globalization;
using System.Text.Json;
using Heimdall.Server.Rendering;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Http;

namespace TestStore.Shared;

public static class StoreView
{
    public const string RegionId = "store-region";
    public static string Money(decimal value) => value.ToString("C", CultureInfo.GetCultureInfo("en-US"));
    public static IHtmlContent Diagnostics(string backend) => FluentHtml.Tag("details", d => d.Class("diagnostics")
        .Tag("summary", s => s.Text("Request details"))
        .P(p => p.Text($"Backend: {backend} · Instance: {Environment.GetEnvironmentVariable("WEBSITE_INSTANCE_ID") ?? Environment.MachineName} · UTC: {DateTimeOffset.UtcNow:HH:mm:ss}")));

    public static IHtmlContent Button(string text, string invocation, StoreRequest payload, string css = "button")
        => FluentHtml.Button(b => b.Type("button").Class(css).Text(text)
            .Heimdall().Click(invocation).Payload(payload).Target("#" + RegionId).SwapOuter().Disable());

    public static IHtmlContent Error(string message) => FluentHtml.Section(s => s.Id(RegionId).Class("empty")
        .H1(h => h.Text("We couldn't update your bag"))
        .P(p => p.Text(message)).A(a => a.Href("/").Text("Start again")));
}
