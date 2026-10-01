using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;
//References
//MongoDB, n.d. Replace Documents [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/update-replace/> [Accessed 21 September 2026].

//MongoDB, n.d. Insert Documents [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/insert/> [Accessed 21 September 2026].

public sealed class WishlistRepository(IMongoDatabase database)
    : MongoRepositoryBase<Wishlist>(database, MongoCollections.Wishlists), IWishlistRepository
{
    public async Task<Wishlist?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
        await Collection
            .Find(Filter.Eq(w => w.ClientId, clientId))
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task SaveAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        wishlist.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(wishlist.Id))
        {
            await Collection.InsertOneAsync(wishlist, cancellationToken: ct);
            return;
        }

        await Collection.ReplaceOneAsync(
            Filter.Eq(w => w.Id, wishlist.Id),
            wishlist,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }
}
