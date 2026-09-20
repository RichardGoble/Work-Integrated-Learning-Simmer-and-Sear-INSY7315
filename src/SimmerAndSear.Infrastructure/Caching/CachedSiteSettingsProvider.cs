using Microsoft.Extensions.Caching.Memory;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Caching;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Cache in-memory in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/performance/caching/memory?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2025. SemaphoreSlim Class [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.threading.semaphoreslim> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed class CachedSiteSettingsProvider(ISiteSettingsRepository repository, IMemoryCache cache)
    : ISiteSettingsProvider
{
    private const string CacheKey = "site-settings";

    
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(1);

   
    private static readonly SemaphoreSlim RefreshGate = new(1, 1);

    public async Task<SiteSettings> CurrentAsync(CancellationToken ct = default)
    {
        if (cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached is not null)
        {
            return cached;
        }

        await RefreshGate.WaitAsync(ct);

        try
        {
           
            if (cache.TryGetValue(CacheKey, out cached) && cached is not null)
            {
                return cached;
            }

            var settings = await repository.GetAsync(ct);

            cache.Set(CacheKey, settings, new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheDuration,

                Priority = CacheItemPriority.High,
                Size = 1
            });

            return settings;
        }
        finally
        {
            RefreshGate.Release();
        }
    }

    public void Invalidate() => cache.Remove(CacheKey);
}
