using SimmerAndSear.Domain.Abstractions;

namespace SimmerAndSear.Domain.Entities;

/// <summary>
/// Singleton - exactly one document, at the fixed id <see cref="SingletonId"/>.
/// Holds the values the client must be able to change without a code change,
/// most importantly the bulk-order threshold that branches checkout to the
/// quotation path, and the address that receives contact notifications.
/// </summary>
public sealed class SiteSettings : IEntity
{
    /// <summary>Fixed document id. There is never more than one of these.</summary>
    public const string SingletonId = "global";

    public string Id { get; set; } = SingletonId;

    /// <summary>
    /// The RECIPIENT of contact notifications, editable by the Sales Admin.
    /// Distinct from the verified sender address, which lives in configuration.
    /// </summary>
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
