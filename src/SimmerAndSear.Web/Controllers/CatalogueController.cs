using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Catalogue;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Web.ViewModels;
using SimmerAndSear.Web.ViewModels.Components;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. Model Binding in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/model-binding?view=aspnetcore-10.0> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public class CatalogueController(CatalogueService catalogue, CurrentUser current) : Controller
{
    public async Task<IActionResult> Index([FromQuery] ProductFilter filter, CancellationToken ct)
    {
        ViewData["Title"] = "Catalogue";

        var page = await catalogue.SearchAsync(filter, CanSeeDrafts, ct);
        var facets = await catalogue.GetFacetsAsync(CanSeeDrafts, ct);

        var cards = page.Items.Select(p => new CardVm(
            Title: p.Name,
            Href: $"/Catalogue/Detail/{p.Id}",
            ImageUrl: p.PrimaryImage?.ThumbnailUrl ?? string.Empty,
            AltText: p.PrimaryImage?.AltText ?? p.Name,
            Meta: Describe(p),
            Badge: p.InStock ? null : "Out of stock",
            Price: Money.Format(p.Price, p.Currency),
            Category: p.Category,
            ProductId: p.Id,
            InStock: p.InStock)).ToList();

        // Carries the current filter into every page link, so paging does not silently
        // drop the filter the visitor applied.
        var pagination = new PaginationVm(page.PageNumber, page.TotalPages, "/Catalogue" + QueryFor(filter));

        ViewData["Facets"] = facets;
        ViewData["Filter"] = filter;

        return View((cards, pagination));
    }

    private static string Describe(ProductSummaryResponse p)
    {
        var parts = new[] { p.Material, p.SizeLabel }.Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join(" • ", parts);
    }

    
    private static string QueryFor(ProductFilter f)
    {
        var parts = new List<string>();

        void Add(string key, string? value)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                parts.Add($"{key}={Uri.EscapeDataString(value)}");
            }
        }

        Add("searchTerm", f.SearchTerm);
        Add("brand", f.Brand);
        Add("category", f.Category);
        Add("cookwareType", f.CookwareType);
        Add("material", f.Material);
        Add("sizeLabel", f.SizeLabel);

        if (f.Sort != ProductSort.Relevance)
        {
            Add("sort", f.Sort.ToString());
        }

        if (f.PageSize != PagedQuery.DefaultPageSize)
        {
            Add("pageSize", f.PageSize.ToString());
        }

        return parts.Count == 0 ? string.Empty : "?" + string.Join("&", parts);
    }

    public async Task<IActionResult> Detail(string id, CancellationToken ct)
    {
        var product = await catalogue.GetAsync(id, CanSeeDrafts, ct);

        if (product is null)
        {
            return NotFound();
        }

        ViewData["Title"] = product.Name;
        return View(product);
    }

    private bool CanSeeDrafts =>
        current.Role is RoleName.MasterAdmin or RoleName.SalesAdmin;
}
