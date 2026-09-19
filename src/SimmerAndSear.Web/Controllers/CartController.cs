using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Web.Security;

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
public class CartController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Your Cart";
        return View();
    }

    [Authorize(Policy = Permissions.Customer)]
    public IActionResult Checkout()
    {
        ViewData["Title"] = "Checkout";
        return View();
    }

    [Authorize(Policy = Permissions.Customer)]
    public IActionResult OrderConfirmation()
    {
        ViewData["Title"] = "Order Confirmed";
        return View();
    }
}
