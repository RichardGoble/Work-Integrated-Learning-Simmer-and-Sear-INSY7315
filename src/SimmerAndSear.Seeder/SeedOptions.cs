namespace SimmerAndSear.Seeder;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Options pattern [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SeedOptions
{
    public const string SectionName = "Seed";
    public bool AccountsOnly { get; set; }
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
