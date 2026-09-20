using System.Security.Cryptography;
using SimmerAndSear.Application.Commerce;

namespace SimmerAndSear.Web.Security;

// ── References ──────────────────────────────────────────────────────────────
//OWASP Foundation, n.d. Session Management - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Session_Management_Cheat_Sheet.html> [Accessed 18 September 2026].
//
//Microsoft, 2026. Use cookie authentication without ASP.NET Core Identity [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0> [Accessed 18 September 2026].
//



public sealed class CartSession(IHttpContextAccessor accessor, CurrentUser current)
{
    public const string CookieName = "__Host-SimmerAndSear-Cart";

    private const int TokenBytes = 24;
    private static readonly int TokenLength = TokenBytes * 2;


    public CartOwner? Resolve()
    {
        if (current.Id is { Length: > 0 } clientId)
        {
            return CartOwner.ForClient(clientId);
        }

        var token = accessor.HttpContext?.Request.Cookies[CookieName];
        return IsWellFormed(token) ? CartOwner.ForSession(token!) : null;
    }

 
    public CartOwner ResolveOrIssue()
    {
        if (Resolve() is { } owner)
        {
            return owner;
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(TokenBytes)).ToLowerInvariant();

        accessor.HttpContext?.Response.Cookies.Append(CookieName, token, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/",
            MaxAge = CartService.AnonymousCartLifetime,

            
            IsEssential = true
        });

        return CartOwner.ForSession(token);
    }


    public void ClearSessionCookie() =>
        accessor.HttpContext?.Response.Cookies.Delete(CookieName, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Lax,
            Path = "/"
        });

    private static bool IsWellFormed(string? token) =>
        token is not null &&
        token.Length == TokenLength &&
        token.All(char.IsAsciiHexDigitLower);
}
