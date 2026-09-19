using Microsoft.AspNetCore.Mvc;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public class NewsController : Controller
{
    [Route("/news")]
    public IActionResult Index()
    {
        ViewData["Title"] = "Journal";
        return View();
    }

    [Route("/news/{id}")]
    public IActionResult Detail(string id = "1")
    {
        ViewData["Title"] = "Article Detail";
        return View();
    }
}
