using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

/// <summary>
/// Access to the single settings document. Singleton at the storage level: the
/// document id is fixed, so a second one cannot be created by accident.
/// </summary>
public interface ISiteSettingsRepository
{
    /// <summary>Returns the stored settings, creating the defaults if absent.</summary>
    Task<SiteSettings> GetAsync(CancellationToken ct = default);

    Task UpdateAsync(SiteSettings settings, CancellationToken ct = default);
}
