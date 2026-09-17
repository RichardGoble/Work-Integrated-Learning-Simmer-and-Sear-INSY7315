using Microsoft.AspNetCore.Mvc;

namespace SimmerAndSear.Web.Controllers;

public class AdminController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Admin Dashboard";
        return View();
    }
}
