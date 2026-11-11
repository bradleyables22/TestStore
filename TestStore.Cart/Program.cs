using Heimdall.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TestStore.Cart.Hosting;
using TestStore.Shared;
using TestStore.Cart.Demo;

var builder = FunctionsApplication.CreateBuilder(args);
builder.ConfigureFunctionsWebApplication();
builder.UseMiddleware<ExposeCorsHeaders>();
builder.Configuration.AddUserSecrets<Program>(optional: true).AddEnvironmentVariables();
builder.Services.AddApplicationDataProtection(builder.Configuration, builder.Environment);

builder.Services.AddSingleton<CartStore>();
builder.Services.AddHeimdallFunctions(options => {
    options.EnableDetailedErrors = builder.Environment.IsDevelopment();
});

if (builder.Configuration.GetValue<bool>("Store:CrossSiteCookies"))
    builder.Services.AddAntiforgery(options => {
        options.Cookie.SameSite = SameSiteMode.None;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

var previews = new PreviewOptions(builder.Configuration.GetValue<bool?>("Store:EnablePreview") ?? builder.Environment.IsDevelopment());
builder.Services.AddSingleton(previews);
if (previews.Enabled)
    builder.Services.AddHeimdallFunctionPages(pages =>
        pages.MapPage("/", (services, context) => PreviewPage.Render(context, "Cart")));

builder.Build().Run();
