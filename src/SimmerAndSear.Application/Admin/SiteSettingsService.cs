using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Admin;

public sealed class SiteSettingsService(ISiteSettingsRepository repository, ISiteSettingsProvider provider)
{
    public async Task<SiteSettingsResponse> GetAsync(CancellationToken ct = default)
    {
        var settings = await provider.CurrentAsync(ct);
        return Map(settings);
    }

    public async Task<SiteSettingsResponse> UpdateAsync(
        UpdateSiteSettingsRequest request, string updatedBy, CancellationToken ct = default)
    {
        var settings = await repository.GetAsync(ct);

        settings.BulkOrderThreshold = request.BulkOrderThreshold;
        settings.DisplayCurrency = request.DisplayCurrency.Trim().ToUpperInvariant();
        settings.ShippingFlatRate = request.ShippingFlatRate;
        settings.QuotationValidityDays = request.QuotationValidityDays;
        settings.ReviewsRequirePurchase = request.ReviewsRequirePurchase;
        settings.SalesNotificationEmail = request.SalesNotificationEmail.Trim();
        settings.MaintenanceMode = request.MaintenanceMode;
        settings.UpdatedBy = updatedBy;
        settings.UpdatedAt = DateTime.UtcNow;

        await repository.UpdateAsync(settings, ct);
        provider.Invalidate();

        return Map(settings);
    }

    private static SiteSettingsResponse Map(SiteSettings s) => new(
        s.BulkOrderThreshold,
        s.DisplayCurrency,
        s.ShippingFlatRate,
        s.QuotationValidityDays,
        s.ReviewsRequirePurchase,
        s.SalesNotificationEmail,
        s.MaintenanceMode,
        s.UpdatedBy,
        s.UpdatedAt);
}
