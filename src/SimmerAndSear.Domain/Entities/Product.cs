using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class Product : IEntity
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Sku { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public string Brand { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string CookwareType { get; set; } = string.Empty;
    public string? Material { get; set; }
    public string? SizeLabel { get; set; }
    public decimal? CapacityLitres { get; set; }

    public decimal Price { get; set; }
    public string Currency { get; set; } = "ZAR";
    public int StockQuantity { get; set; }
    public bool IsPublished { get; set; }

    public double AverageRating { get; set; }
    public int ReviewCount { get; set; }

    public List<ProductImage> Images { get; set; } = [];

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool HasRequiredImages => Images.Count > 0;

    public ProductImage? PrimaryImage =>
        Images.FirstOrDefault(i => i.IsPrimary)
        ?? Images.OrderBy(i => i.DisplayOrder).FirstOrDefault();
}

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class ProductImage
{
    public string ProductImageId { get; set; } = string.Empty;
    public string MediaAssetId { get; set; } = string.Empty;
    public string AltText { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}
