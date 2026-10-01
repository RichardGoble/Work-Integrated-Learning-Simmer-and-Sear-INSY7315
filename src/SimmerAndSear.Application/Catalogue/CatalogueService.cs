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

    public async Task<ProductDetailResponse> CreateAsync(
        CreateProductRequest request, CancellationToken ct = default)
    {
        var product = new Product();
        Apply(request, product);

        await products.CreateAsync(product, ct);
        return Describe(product);
    }

    public async Task<ProductDetailResponse?> UpdateAsync(
        string id, UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);

        if (product is null)
        {
            return null;
        }

        Apply(request, product);
        await products.UpdateAsync(product, ct);

        return Describe(product);
    }

    
    public async Task<ProductDetailResponse?> DeleteAsync(string id, CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);

        if (product is null)
        {
            return null;
        }

        await products.DeleteAsync(id, ct);
        return Describe(product);
    }


    private static void Apply(CreateProductRequest r, Product product)
    {
        if (r.Images.Count == 0)
        {
            throw new ProductRuleException("A product needs at least one image.");
        }

        if (r.Images.Select(i => i.MediaAssetId.Trim()).Distinct().Count() != r.Images.Count)
        {
            throw new ProductRuleException("The same image has been added more than once.");
        }

        product.Name = r.Name.Trim();
        product.Sku = r.Sku.Trim().ToUpperInvariant();
        product.Description = r.Description.Trim();
        product.Brand = r.Brand.Trim();
        product.Category = r.Category.Trim();
        product.CookwareType = r.CookwareType.Trim();
        product.Material = Clean(r.Material);
        product.SizeLabel = Clean(r.SizeLabel);
        product.CapacityLitres = r.CapacityLitres;
        product.Price = r.Price;
        product.StockQuantity = r.StockQuantity;
        product.IsPublished = r.IsPublished;

        var ordered = r.Images.OrderBy(i => i.DisplayOrder).ToList();
        var primary = ordered.FindIndex(i => i.IsPrimary);

        if (primary < 0)
        {
            primary = 0;
        }

        product.Images =
        [
            .. ordered.Select((i, index) => new ProductImage
            {
                ProductImageId = Guid.NewGuid().ToString("N"),
                MediaAssetId = i.MediaAssetId.Trim(),
                AltText = i.AltText.Trim(),
                DisplayOrder = index,
                IsPrimary = index == primary
            })
        ];
    }

  
    public async Task<(ProductDetailResponse? Product, bool Removed)> RemoveImageAsync(
        string id, string mediaAssetId, CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);

        if (product is null)
        {
            return (null, false);
        }

        var image = product.Images.Find(i => i.MediaAssetId == mediaAssetId);

        if (image is null)
        {
            return (Describe(product), false);
        }

        if (product.Images.Count <= 1)
        {
            throw new ProductRuleException("A product needs at least one image.");
        }

        product.Images.Remove(image);
        Renumber(product);

        await products.UpdateAsync(product, ct);
        return (Describe(product), true);
    }

    public async Task<ProductDetailResponse?> SetPrimaryImageAsync(
        string id, string mediaAssetId, CancellationToken ct = default)
    {
        var product = await products.GetAsync(id, ct);

        if (product is null || product.Images.TrueForAll(i => i.MediaAssetId != mediaAssetId))
        {
            return null;
        }

        foreach (var image in product.Images)
        {
            image.IsPrimary = image.MediaAssetId == mediaAssetId;
        }

        Renumber(product);

        await products.UpdateAsync(product, ct);
        return Describe(product);
    }

    // The primary image leads the gallery, and exactly one must hold the flag even
    // after the one that had it is deleted.
    private static void Renumber(Product product)
    {
        var ordered = product.Images.OrderByDescending(i => i.IsPrimary)
            .ThenBy(i => i.DisplayOrder)
            .ToList();

        for (var index = 0; index < ordered.Count; index++)
        {
            ordered[index].DisplayOrder = index;
            ordered[index].IsPrimary = index == 0;
        }

        product.Images = ordered;
    }

    private static string? Clean(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

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
