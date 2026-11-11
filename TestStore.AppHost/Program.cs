using Heimdall.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestStore.AppHost.Hosting;
using TestStore.Shared;


var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.UseMiddleware<ExposeCorsHeaders>();
builder.Configuration.AddUserSecrets<Program>(optional: true).AddEnvironmentVariables();
builder.Services.AddApplicationDataProtection(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<StoreOptions>();

builder.Services.AddHeimdallFunctions(options => {
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

if (builder.Configuration.GetValue<bool>("Store:CrossSiteCookies"))
    builder.Services.AddAntiforgery(options => {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });
builder.Services.AddHeimdallFunctionPages(pages => {
    pages.MapPage("/", (services, context) => StoreShell.Page(context, services.GetRequiredService<StoreOptions>(), StorePageRequest.FromPage(context), services.GetRequiredService<StoreOptions>().CatalogOrigin, "Catalog", "catalog.load"));
    pages.MapPage("/cart", (services, context) => StoreShell.Page(context, services.GetRequiredService<StoreOptions>(), StorePageRequest.FromPage(context), services.GetRequiredService<StoreOptions>().CartOrigin, "Cart", "cart.load"));
});
builder.Build().Run();
