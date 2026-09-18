using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface ISiteSettingsRepository
{
    Task<SiteSettings> GetAsync(CancellationToken ct = default);

    Task UpdateAsync(SiteSettings settings, CancellationToken ct = default);
}
