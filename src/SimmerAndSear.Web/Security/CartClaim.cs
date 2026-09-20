using Microsoft.Extensions.Logging;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Use cookie authentication without ASP.NET Core Identity [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class CartClaim(
    CartService carts, CartSession session, ILogger<CartClaim> logger)
{
    
    public async Task OnSignedInAsync(User user, CancellationToken ct = default)
    {
        
        if (user.Role is not RoleName.Customer)
        {
            return;
        }

        if (session.SessionToken is not { } token)
        {
            return;
        }

        try
        {
            await carts.ClaimAsync(user.Id, token, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Could not claim the anonymous cart for {UserId}.", user.Id);
        }

       
        session.ClearSessionCookie();
    }
}
