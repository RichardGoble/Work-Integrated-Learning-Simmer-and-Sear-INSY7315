using Microsoft.AspNetCore.Mvc;

namespace SimmerAndSear.Web.Controllers;

public class AuthController : Controller
{
    public IActionResult Login(bool error = false)
    {
        ViewData["Title"] = "Sign In / Register";
        ViewData["Error"] = error;
        return View();
    }

    public IActionResult Profile()
    {
        ViewData["Title"] = "My Profile";
        return View();
    }
}
