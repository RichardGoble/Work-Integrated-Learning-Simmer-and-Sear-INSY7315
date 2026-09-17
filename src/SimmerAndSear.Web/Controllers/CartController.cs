using Microsoft.AspNetCore.Mvc;

namespace SimmerAndSear.Web.Controllers;

public class CartController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Your Cart";
        return View();
    }

    public IActionResult Checkout()
    {
        ViewData["Title"] = "Checkout";
        return View();
    }

    public IActionResult OrderConfirmation()
    {
        ViewData["Title"] = "Order Confirmed";
        return View();
    }
}
