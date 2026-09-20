using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2026. Cache in-memory in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public interface ISiteSettingsProvider
{
    Task<SiteSettings> CurrentAsync(CancellationToken ct = default);

    void Invalidate();
}
