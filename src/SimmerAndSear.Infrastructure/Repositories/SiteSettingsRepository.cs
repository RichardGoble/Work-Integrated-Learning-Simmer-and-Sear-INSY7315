using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SiteSettingsRepository(IMongoDatabase database)
    : MongoRepositoryBase<SiteSettings>(database, MongoCollections.SiteSettings), ISiteSettingsRepository
{
    public async Task<SiteSettings> GetAsync(CancellationToken ct = default)
    {
        var existing = await Collection
            .Find(Filter.Eq(s => s.Id, SiteSettings.SingletonId))
            .Limit(1)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
        {
            return existing;
        }
        var defaults = new SiteSettings
        {
            Id = SiteSettings.SingletonId,
            UpdatedBy = "system",
            UpdatedAt = DateTime.UtcNow
        };

        await Collection.InsertOneAsync(defaults, cancellationToken: ct);
        return defaults;
    }

    public async Task UpdateAsync(SiteSettings settings, CancellationToken ct = default)
    {
        settings.Id = SiteSettings.SingletonId;
        settings.UpdatedAt = DateTime.UtcNow;

        await Collection.ReplaceOneAsync(
            Filter.Eq(s => s.Id, SiteSettings.SingletonId),
            settings,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }
}
