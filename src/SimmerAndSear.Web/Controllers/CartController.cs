using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

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
