using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
//
//MongoDB, n.d. Update Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/update/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class GalleryRepository(IMongoDatabase database)
    : MongoRepositoryBase<GalleryImage>(database, MongoCollections.GalleryImages), IGalleryRepository
{
    public async Task<List<GalleryImage>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Filter.Empty).Sort(SortBy.Ascending(g => g.Slot)).ToListAsync(ct);

    public async Task<GalleryImage?> GetBySlotAsync(int slot, CancellationToken ct = default)
    {
        if (!GalleryImage.IsValidSlot(slot)) return null;
        return await Collection.Find(Filter.Eq(g => g.Slot, slot)).FirstOrDefaultAsync(ct);
    }


    public async Task SetAsync(GalleryImage image, CancellationToken ct = default)
    {
        if (!GalleryImage.IsValidSlot(image.Slot))
        {
            throw new ArgumentOutOfRangeException(
                nameof(image), $"Slot must be between 1 and {GalleryImage.SlotCount}.");
        }

        image.UpdatedAt = DateTime.UtcNow;

        await Collection.UpdateOneAsync(
            Filter.Eq(g => g.Slot, image.Slot),
            Update
                .Set(g => g.Slot, image.Slot)
                .Set(g => g.MediaAssetId, image.MediaAssetId)
                .Set(g => g.Caption, image.Caption)
                .Set(g => g.UpdatedBy, image.UpdatedBy)
                .Set(g => g.UpdatedAt, image.UpdatedAt),
            new UpdateOptions { IsUpsert = true },
            ct);
    }

    public async Task ClearAsync(int slot, CancellationToken ct = default)
    {
        if (!GalleryImage.IsValidSlot(slot)) return;
        await Collection.DeleteOneAsync(Filter.Eq(g => g.Slot, slot), ct);
    }
}
