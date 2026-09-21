using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.UnitTests.Admin;

// ── References ──────────────────────────────────────────────────────────────
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 21 September 2026].
//
//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 21 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SiteSettingsServiceTests
{
    private readonly MemorySiteSettingsRepository _repository = new();
    private readonly MemorySiteSettingsProvider _provider;

    public SiteSettingsServiceTests()
    {
        _provider = new MemorySiteSettingsProvider(_repository);
    }

    [Fact]
    public async Task UpdateAsync_updates_all_settings_and_invalidates_cache()
    {
        var service = new SiteSettingsService(_repository, _provider);

        var request = new UpdateSiteSettingsRequest
        {
            BulkOrderThreshold = 25,
            DisplayCurrency = "zar",
            ShippingFlatRate = 120.50m,
            QuotationValidityDays = 45,
            ReviewsRequirePurchase = false,
            SalesNotificationEmail = "sales@simmersear.co.za",
            MaintenanceMode = true
        };

        var response = await service.UpdateAsync(request, "admin-user");

        response.BulkOrderThreshold.ShouldBe(25);
        response.DisplayCurrency.ShouldBe("ZAR");
        response.ShippingFlatRate.ShouldBe(120.50m);
        response.QuotationValidityDays.ShouldBe(45);
        response.ReviewsRequirePurchase.ShouldBeFalse();
        response.SalesNotificationEmail.ShouldBe("sales@simmersear.co.za");
        response.MaintenanceMode.ShouldBeTrue();
        response.UpdatedBy.ShouldBe("admin-user");
        _provider.InvalidateCallCount.ShouldBe(1);
    }

    private sealed class MemorySiteSettingsRepository : ISiteSettingsRepository
    {
        public SiteSettings Settings { get; set; } = new();

        public Task<SiteSettings> GetAsync(CancellationToken ct = default) =>
            Task.FromResult(Settings);

        public Task UpdateAsync(SiteSettings settings, CancellationToken ct = default)
        {
            Settings = settings;
            return Task.CompletedTask;
        }
    }

    private sealed class MemorySiteSettingsProvider(ISiteSettingsRepository repository) : ISiteSettingsProvider
    {
        public int InvalidateCallCount { get; private set; }

        public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) =>
            repository.GetAsync(ct);

        public void Invalidate()
        {
            InvalidateCallCount++;
        }
    }
}
