using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[Authorize(Policy = Permissions.Customer)]
public class WishlistController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "My Wishlist";
        return View();
    }
}
