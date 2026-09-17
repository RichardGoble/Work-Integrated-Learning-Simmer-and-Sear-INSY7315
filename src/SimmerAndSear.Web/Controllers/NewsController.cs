using Microsoft.AspNetCore.Mvc;

namespace SimmerAndSear.Web.Controllers;

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
