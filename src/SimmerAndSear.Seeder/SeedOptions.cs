namespace SimmerAndSear.Seeder;

/// <summary>Bound from the "Seed" configuration section.</summary>
public sealed class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>
    /// Development passwords for the three demonstration accounts. Read from
    /// configuration, never hard-coded and never committed: set them with
    /// user-secrets locally, or Seed__MasterAdminPassword and friends in CI.
    /// </summary>
    public string MasterAdminPassword { get; set; } = string.Empty;

    public string SalesAdminPassword { get; set; } = string.Empty;

    public string CustomerPassword { get; set; } = string.Empty;

    public string MasterAdminEmail { get; set; } = "master@simmerandsear.local";

    public string SalesAdminEmail { get; set; } = "sales@simmerandsear.local";

    public string CustomerEmail { get; set; } = "customer@simmerandsear.local";

    /// <summary>Folder holding the product photography named in products.json.</summary>
    public string ImagesPath { get; set; } = Path.Combine("SeedData", "images");

    /// <summary>Bulk-order threshold written into site settings on first run, in rand.</summary>
    public decimal BulkOrderThreshold { get; set; } = 5000m;

    public decimal ShippingFlatRate { get; set; } = 120m;

    public string SalesNotificationEmail { get; set; } = "sales@simmerandsear.local";
}
