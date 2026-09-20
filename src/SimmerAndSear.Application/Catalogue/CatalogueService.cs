using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Catalogue;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 20 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class CatalogueService(IProductRepository products)
{
    public async Task<Page<ProductSummaryResponse>> SearchAsync(
        ProductFilter filter, bool includeUnpublished = false, CancellationToken ct = default)
    {
        var safe = filter with { PublishedOnly = !includeUnpublished };
        var page = await products.SearchAsync(safe, ct);

        return new Page<ProductSummaryResponse>(
            [.. page.Items.Select(Summarise)],
            page.Total,
            page.PageNumber,
            page.PageSize);
    }

    public async Task<ProductDetailResponse?> GetAsync(
        string id, bool includeUnpublished = false, CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);

 
        return product is null || (!product.IsPublished && !includeUnpublished)
            ? null
            : Describe(product);
    }

    public Task<CatalogueFacets> GetFacetsAsync(
        bool includeUnpublished = false, CancellationToken ct = default) =>
        products.GetFacetsAsync(!includeUnpublished, ct);

    private static ProductSummaryResponse Summarise(Product p) => new(
        p.Id,
        p.Name,
        p.Sku,
        p.Brand,
        p.Category,
        p.CookwareType,
        p.Material,
        p.SizeLabel,
        p.Price,
        p.Currency,
        p.StockQuantity,
        p.IsPublished,
        p.AverageRating,
        p.ReviewCount,
        ToImage(p.PrimaryImage));

    private static ProductDetailResponse Describe(Product p) => new(
        p.Id,
        p.Name,
        p.Sku,
        p.Description,
        p.Brand,
        p.Category,
        p.CookwareType,
        p.Material,
        p.SizeLabel,
        p.CapacityLitres,
        p.Price,
        p.Currency,
        p.StockQuantity,
        p.IsPublished,
        p.AverageRating,
        p.ReviewCount,
        [.. p.Images
             .OrderByDescending(i => i.IsPrimary)
             .ThenBy(i => i.DisplayOrder)
             .Select(i => new ProductImageResponse(i.MediaAssetId, i.AltText, i.DisplayOrder, i.IsPrimary))]);

    private static ProductImageResponse? ToImage(ProductImage? image) =>
        image is null || string.IsNullOrWhiteSpace(image.MediaAssetId)
            ? null
            : new ProductImageResponse(image.MediaAssetId, image.AltText, image.DisplayOrder, image.IsPrimary);
}
