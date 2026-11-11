using Microsoft.Extensions.Configuration;
namespace TestStore.Shared;

public sealed class StoreOptions(IConfiguration configuration)
{
    public string CatalogOrigin { get; } = Origin(configuration["Store:CatalogOrigin"], "http://localhost:7072");
    public string CartOrigin { get; } = Origin(configuration["Store:CartOrigin"], "http://localhost:7073");
    private static string Origin(string? value, string fallback)
    {
        var uri = new Uri(string.IsNullOrWhiteSpace(value) ? fallback : value);
        if (uri.Scheme is not ("http" or "https") || uri.AbsolutePath != "/" || uri.Query != "" || uri.Fragment != "" || uri.UserInfo != "")
            throw new InvalidOperationException("Store origins must be absolute HTTP(S) origins without paths, queries, or credentials.");
        return uri.GetLeftPart(UriPartial.Authority);
    }
}

