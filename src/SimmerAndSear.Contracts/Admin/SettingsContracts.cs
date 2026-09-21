using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Admin;

public sealed class UpdateSiteSettingsRequest
{
    [EmailAddress]
    public string SalesNotificationEmail { get; set; } = string.Empty;

    [Range(0.01, 10_000_000.00)]
    public decimal BulkOrderThreshold { get; set; }

    [StringLength(10)]
    public string DisplayCurrency { get; set; } = "ZAR";

    [Range(0.00, 10_000.00)]
    public decimal ShippingFlatRate { get; set; }

    [Range(1, 365)]
    public int QuotationValidityDays { get; set; } = 30;

    public bool ReviewsRequirePurchase { get; set; }

    public bool MaintenanceMode { get; set; }
}

public sealed record SiteSettingsResponse(
    decimal BulkOrderThreshold,
    string DisplayCurrency,
    decimal ShippingFlatRate,
    int QuotationValidityDays,
    bool ReviewsRequirePurchase,
    string SalesNotificationEmail,
    bool MaintenanceMode,
    string UpdatedBy,
    DateTime UpdatedAt);
