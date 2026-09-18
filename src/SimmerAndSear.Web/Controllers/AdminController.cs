using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[Authorize(Policy = Permissions.Staff)]
public class AdminController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Admin Dashboard";
        return View();
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login()
    {
        return RedirectToAction("Login", "Auth", new { returnUrl = "/Admin" });
    }
}
