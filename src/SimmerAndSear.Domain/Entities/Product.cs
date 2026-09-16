using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;

/// <summary>
/// A catalogue listing. Images are embedded because they are never queried
/// independently of their product, matching the Task 1 document schema.
/// </summary>
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

    /// <summary>
    /// A listing must carry at least one image. Enforced here, again at the API
    /// boundary, and again by the collection's $jsonSchema validator - defence in
    /// depth on a rule the client asked for explicitly.
    /// </summary>
    public bool HasRequiredImages => Images.Count > 0;

    public ProductImage? PrimaryImage =>
        Images.FirstOrDefault(i => i.IsPrimary)
        ?? Images.OrderBy(i => i.DisplayOrder).FirstOrDefault();
}

public sealed class ProductImage
{
    public string ProductImageId { get; set; } = string.Empty;
    public string MediaAssetId { get; set; } = string.Empty;

    /// <summary>Required, not optional - accessibility is not a later pass.</summary>
    public string AltText { get; set; } = string.Empty;

    public int DisplayOrder { get; set; }
    public bool IsPrimary { get; set; }
}
