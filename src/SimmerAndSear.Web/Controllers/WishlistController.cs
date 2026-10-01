using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Wishlist;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Web.ViewModels;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
[Authorize(Policy = Permissions.Customer)]
public class WishlistController(
    WishlistService wishlists,
    CurrentUser current,
    ISiteSettingsProvider settings) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "My Wishlist";

        var site = await settings.CurrentAsync(ct);

        return View(new WishlistPageVm(
            await wishlists.GetAsync(current.Id!, ct),
            site.BulkOrderThreshold,
            site.DisplayCurrency));
    }
}
