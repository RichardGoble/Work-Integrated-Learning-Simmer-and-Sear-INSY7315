using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SiteSettings : IEntity
{
    public const string SingletonId = "global";

    public string Id { get; set; } = SingletonId;
    public string SalesNotificationEmail { get; set; } = string.Empty;

    public decimal BulkOrderThreshold { get; set; }
    public string DisplayCurrency { get; set; } = "ZAR";
    public decimal ShippingFlatRate { get; set; }
    public int QuotationValidityDays { get; set; } = 30;
    public bool ReviewsRequirePurchase { get; set; } = true;
    public bool MaintenanceMode { get; set; }

    public string UpdatedBy { get; set; } = string.Empty;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
