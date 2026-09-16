using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

/// <summary>Metadata for binaries held in GridFS.</summary>
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

    /// <summary>
    /// Candidate orphans: assets older than the cut-off that no product image
    /// references. Uploading a file and writing its metadata are two operations,
    /// so a failure between them leaks a blob; this is how the sweeper finds them.
    /// </summary>
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
