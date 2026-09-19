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
public sealed class MediaAssetRepository(IMongoDatabase database)
    : MongoRepositoryBase<MediaAsset>(database, MongoCollections.MediaAssets), IMediaAssetRepository
{
    public async Task<MediaAsset?> GetAsync(string id, CancellationToken ct = default) =>
        await Collection.Find(Filter.Eq(m => m.Id, id)).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<string> CreateAsync(MediaAsset asset, CancellationToken ct = default)
    {
        asset.UploadedAt = DateTime.UtcNow;
        await Collection.InsertOneAsync(asset, cancellationToken: ct);
        return asset.Id;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default) =>
        await Collection.DeleteOneAsync(Filter.Eq(m => m.Id, id), ct);
    public async Task<IReadOnlyList<MediaAsset>> FindOrphansAsync(
        DateTime olderThanUtc, CancellationToken ct = default)
    {
        var referenced = await Database
            .GetCollection<Product>(MongoCollections.Products)
            .Distinct<string>("images.mediaAssetId", Builders<Product>.Filter.Empty, cancellationToken: ct)
            .ToListAsync(ct);

        return await Collection
            .Find(Filter.And(
                Filter.Lt(m => m.UploadedAt, olderThanUtc),
                Filter.Nin(m => m.Id, referenced)))
            .Limit(500)
            .ToListAsync(ct);
    }
}
