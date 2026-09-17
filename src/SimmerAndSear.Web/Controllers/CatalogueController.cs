using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Web.ViewModels.Components;
using System.Collections.Generic;

namespace SimmerAndSear.Web.Controllers;

public class CatalogueController : Controller
{
    public IActionResult Index(string? filter)
    {
        ViewData["Title"] = "Catalogue";
        
        var products = new List<CardVm>();
        var pagination = new PaginationVm(1, 1, "/Catalogue");
        
        if (filter == "empty")
        {
            // Empty state view
            return View((products, pagination));
        }

        products.AddRange(new[]
        {
            new CardVm("[Dynamic Product Title 1]", "/Catalogue/Detail", "/images/skillet.jpg", "Product Image", "[Material] • [Price]", "[Badge]"),
            new CardVm("[Dynamic Product Title 2]", "/Catalogue/Detail", "/images/dutch_oven.jpg", "Product Image", "[Material] • [Price]", null),
            new CardVm("[Dynamic Product Title 3]", "/Catalogue/Detail", "/images/saucepan.jpg", "Product Image", "[Material] • [Price]", null),
            new CardVm("[Dynamic Product Title 4]", "/Catalogue/Detail", "/images/crepe.jpg", "Product Image", "[Material] • [Price]", null),
            new CardVm("[Dynamic Product Title 5]", "/Catalogue/Detail", "/images/wok.jpg", "Product Image", "[Material] • [Price]", null),
            new CardVm("[Dynamic Product Title 6]", "/Catalogue/Detail", "/images/stockpot.jpg", "Product Image", "[Material] • [Price]", null),
        });

        pagination = new PaginationVm(1, 3, "/Catalogue");
        return View((products, pagination));
    }

    public IActionResult Detail()
    {
        ViewData["Title"] = "Classic Cast Iron Skillet";
        
        var breadcrumbs = new BreadcrumbVm(new List<BreadcrumbItem>
        {
            new BreadcrumbItem("Home", "/"),
            new BreadcrumbItem("Catalogue", "/Catalogue"),
            new BreadcrumbItem("Cast Iron", "/Catalogue?category=Cast Iron"),
            new BreadcrumbItem("Classic Cast Iron Skillet", "/Catalogue/Detail")
        });

        return View(breadcrumbs);
    }
}
