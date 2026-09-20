using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Catalogue;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed class ProductImageRequest
{
    [Required, StringLength(64, MinimumLength = 1)]
    public string MediaAssetId { get; set; } = string.Empty;

  
    [Required, StringLength(250, MinimumLength = 3)]
    public string AltText { get; set; } = string.Empty;

    [Range(0, 50)]
    public int DisplayOrder { get; set; }

    public bool IsPrimary { get; set; }
}

public class CreateProductRequest
{
    [Required, StringLength(160, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, StringLength(40, MinimumLength = 2)]
    public string Sku { get; set; } = string.Empty;

    [Required, StringLength(4000, MinimumLength = 10)]
    public string Description { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 1)]
    public string Brand { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 1)]
    public string Category { get; set; } = string.Empty;

    [Required, StringLength(80, MinimumLength = 1)]
    public string CookwareType { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Material { get; set; }

    [StringLength(40)]
    public string? SizeLabel { get; set; }

    [Range(0.0, 1000.0)]
    public decimal? CapacityLitres { get; set; }

    [Range(0.01, 1_000_000.00)]
    public decimal Price { get; set; }

    [Range(0, 1_000_000)]
    public int StockQuantity { get; set; }

    public bool IsPublished { get; set; }


    [MaxLength(12)]
    public List<ProductImageRequest> Images { get; set; } = [];
}


public sealed class UpdateProductRequest : CreateProductRequest;
