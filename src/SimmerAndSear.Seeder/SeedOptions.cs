namespace SimmerAndSear.Seeder;
public sealed class SeedOptions
{
    public const string SectionName = "Seed";
    public string MasterAdminPassword { get; set; } = string.Empty;

    public string SalesAdminPassword { get; set; } = string.Empty;

    public string CustomerPassword { get; set; } = string.Empty;

    public string MasterAdminEmail { get; set; } = "master@simmerandsear.local";

    public string SalesAdminEmail { get; set; } = "sales@simmerandsear.local";

    public string CustomerEmail { get; set; } = "customer@simmerandsear.local";
    public string ImagesPath { get; set; } = Path.Combine("SeedData", "images");
    public decimal BulkOrderThreshold { get; set; } = 5000m;

    public decimal ShippingFlatRate { get; set; } = 120m;

    public string SalesNotificationEmail { get; set; } = "sales@simmerandsear.local";
}
