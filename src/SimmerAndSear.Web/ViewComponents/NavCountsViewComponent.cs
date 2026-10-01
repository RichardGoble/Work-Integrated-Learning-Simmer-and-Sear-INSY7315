using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Application.Wishlist;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.ViewComponents;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. View components in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/view-components?view=aspnetcore-10.0> [Accessed 30 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 30 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed record NavCounts(int CartItems, int WishlistItems);


public sealed class NavCountsViewComponent(
    CartService carts,
    CartSession session,
    WishlistService wishlists,
    CurrentUser current,
    ILogger<NavCountsViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync()
    {
        try
        {
            var cart = await carts.ViewAsync(session.Resolve(), HttpContext.RequestAborted);

            // The wishlist needs an account; an anonymous visitor simply has none.
            var wishlist = current.Role == RoleName.Customer && current.Id is { Length: > 0 } clientId
                ? (await wishlists.GetAsync(clientId, HttpContext.RequestAborted)).Count
                : 0;

            return View(new NavCounts(cart.TotalQuantity, wishlist));
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not read the navigation counts; rendering without badges.");
            return View(new NavCounts(0, 0));
        }
    }
}
