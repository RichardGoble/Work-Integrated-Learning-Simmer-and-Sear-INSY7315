using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

/// <summary>
/// Singleton - the id is the fixed literal "global", so a second settings
/// document cannot be created even by accident.
/// </summary>
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

        // First run: materialise the defaults so callers never have to handle null
        // and the admin screen always has something to edit.
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
