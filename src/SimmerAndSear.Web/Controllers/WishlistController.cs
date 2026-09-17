using Microsoft.AspNetCore.Mvc;

namespace SimmerAndSear.Web.Controllers;

public class WishlistController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "My Wishlist";
        return View();
    }
}
