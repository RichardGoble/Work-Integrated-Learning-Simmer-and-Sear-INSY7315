using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
public interface ISiteSettingsRepository
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);

    Task UpdateAsync(SiteSettings settings, CancellationToken ct = default);
}
