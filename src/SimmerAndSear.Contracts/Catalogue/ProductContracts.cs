using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Catalogue;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 20 September 2026].
//
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed record ProductImageResponse(
    string MediaAssetId,
    string AltText,
    int DisplayOrder,
    bool IsPrimary)
{
    public string Url => $"/api/media/{MediaAssetId}";

    public string ThumbnailUrl => $"/api/media/{MediaAssetId}/thumb";
}

public sealed record ProductSummaryResponse(
    string Id,
    string Name,
    string Sku,
    string Brand,
    string Category,
    string CookwareType,
    string? Material,
    string? SizeLabel,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsPublished,
    double AverageRating,
    int ReviewCount,
    ProductImageResponse? PrimaryImage)
{
    public bool InStock => StockQuantity > 0;
}


public sealed record ProductDetailResponse(
    string Id,
    string Name,
    string Sku,
    string Description,
    string Brand,
    string Category,
    string CookwareType,
    string? Material,
    string? SizeLabel,
    decimal? CapacityLitres,
    decimal Price,
    string Currency,
    int StockQuantity,
    bool IsPublished,
    double AverageRating,
    int ReviewCount,
    IReadOnlyList<ProductImageResponse> Images)
{
    public bool InStock => StockQuantity > 0;

    public ProductImageResponse? PrimaryImage =>
        Images.FirstOrDefault(i => i.IsPrimary) ?? Images.FirstOrDefault();
}

public sealed record CatalogueFacets(
    IReadOnlyList<string> Brands,
    IReadOnlyList<string> Categories,
    IReadOnlyList<string> CookwareTypes,
    IReadOnlyList<string> Materials,
    IReadOnlyList<string> SizeLabels)
{
    public static CatalogueFacets Empty => new([], [], [], [], []);
}
