using Microsoft.AspNetCore.Http;
namespace TestStore.Shared;
public static class StorePageRequest
{
    public static StoreRequest FromPage(HttpContext context)
    {
        var id = context.Request.Cookies["TestStore.Cart"];
        if (!Guid.TryParseExact(id, "N", out _)) id = Guid.NewGuid().ToString("N");
        context.Response.Cookies.Append("TestStore.Cart", id!, new CookieOptions
        { HttpOnly = true, Secure = context.Request.IsHttps, SameSite = SameSiteMode.Lax, Path = "/", MaxAge = TimeSpan.FromDays(1) });
        return new() { CartId = id!, CartPath = "/cart" };
    }
}
