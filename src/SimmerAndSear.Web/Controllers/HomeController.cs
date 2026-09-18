using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Web.ViewModels.Components;
using System.Collections.Generic;

namespace SimmerAndSear.Web.Controllers;

public class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Premium Cookware";
        var featuredProducts = new List<CardVm>
        {
            new CardVm("[Dynamic Product Title 1]", "/Catalogue/Detail", "/images/skillet.jpg", "Product Image", "[Material] • [Price]", "[Badge]"),
            new CardVm("[Dynamic Product Title 2]", "/Catalogue/Detail", "/images/dutch_oven.jpg", "Product Image", "[Material] • [Price]", null),
            new CardVm("[Dynamic Product Title 3]", "/Catalogue/Detail", "/images/saucepan.jpg", "Product Image", "[Material] • [Price]", null)
        };

        return View(featuredProducts);
    }

    [Route("/About")]
    public IActionResult About()
    {
        ViewData["Title"] = "About Simmer & Sear";
        return View();
    }

    [Route("/Contact")]
    public IActionResult Contact()
    {
        ViewData["Title"] = "Contact Us";
        return View();
    }

    [Route("/Privacy")]
    public IActionResult Privacy()
    {
        ViewData["Title"] = "Privacy Policy";
        return View();
    }
}
