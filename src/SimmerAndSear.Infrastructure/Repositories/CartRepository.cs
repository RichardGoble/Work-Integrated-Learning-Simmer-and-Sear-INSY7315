using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class CartRepository(IMongoDatabase database)
    : MongoRepositoryBase<Cart>(database, MongoCollections.Carts), ICartRepository
{
    public async Task<Cart?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
        await Collection
            .Find(Filter.Eq(c => c.ClientId, clientId))
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct = default) =>
        await Collection
            .Find(Filter.And(
                Filter.Eq(c => c.SessionId, sessionId),
         
                Filter.Eq(c => c.ClientId, null)))
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public async Task SaveAsync(Cart cart, CancellationToken ct = default)
    {
        cart.UpdatedAt = DateTime.UtcNow;

        if (string.IsNullOrEmpty(cart.Id))
        {
            await Collection.InsertOneAsync(cart, cancellationToken: ct);
            return;
        }

        await Collection.ReplaceOneAsync(
            Filter.Eq(c => c.Id, cart.Id),
            cart,
            new ReplaceOptions { IsUpsert = true },
            ct);
    }

    public async Task DeleteAsync(string cartId, CancellationToken ct = default) =>
        await Collection.DeleteOneAsync(Filter.Eq(c => c.Id, cartId), ct);
}
