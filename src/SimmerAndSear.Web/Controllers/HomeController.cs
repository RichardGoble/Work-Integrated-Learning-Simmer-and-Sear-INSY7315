using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Catalogue;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Web.ViewModels;
using SimmerAndSear.Web.ViewModels.Components;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public class HomeController(CatalogueService catalogue) : Controller
{
    private const int FeaturedCount = 3;

    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Premium Cookware";

        
        var page = await catalogue.SearchAsync(
            new ProductFilter { PageSize = FeaturedCount, Sort = ProductSort.Newest }, ct: ct);

        var featured = page.Items.Select(p => new CardVm(
            Title: p.Name,
            Href: $"/Catalogue/Detail/{p.Id}",
            ImageUrl: p.PrimaryImage?.ThumbnailUrl ?? string.Empty,
            AltText: p.PrimaryImage?.AltText ?? p.Name,
            Meta: string.Join(" • ", new[] { p.Material, p.SizeLabel }.Where(s => !string.IsNullOrWhiteSpace(s))),
            Badge: p.InStock ? null : "Out of stock",
            Price: Money.Format(p.Price, p.Currency),
            Category: p.Category,
            ProductId: p.Id,
            InStock: p.InStock)).ToList();

        return View(featured);
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
