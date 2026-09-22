using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class OrderRepository(IMongoDatabase database)
    : MongoRepositoryBase<Order>(database, MongoCollections.Orders), IOrderRepository
{
    public async Task<Order?> GetAsync(string id, CancellationToken ct = default) =>
        !IsValidId(id)
            ? null
            : await Collection.Find(Filter.Eq(o => o.Id, id)).Limit(1).FirstOrDefaultAsync(ct);


    public async Task<Order?> GetForClientAsync(string id, string clientId, CancellationToken ct = default) =>
        !IsValidId(id)
            ? null
            : await Collection
            .Find(Filter.And(
                Filter.Eq(o => o.Id, id),
                Filter.Eq(o => o.ClientId, clientId)))
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public Task<Page<Order>> ListForClientAsync(
        string clientId, int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(
            Filter.Eq(o => o.ClientId, clientId),
            SortBy.Descending(o => o.PlacedAt),
            pageNumber,
            pageSize,
            ct: ct);

    public Task<Page<Order>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Empty, SortBy.Descending(o => o.PlacedAt), pageNumber, pageSize, ct: ct);

    public async Task<string> CreateAsync(Order order, CancellationToken ct = default)
    {
        order.PlacedAt = DateTime.UtcNow;
        order.UpdatedAt = order.PlacedAt;

        await Collection.InsertOneAsync(order, cancellationToken: ct);
        return order.Id;
    }

    public async Task UpdateAsync(Order order, CancellationToken ct = default)
    {
        order.UpdatedAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(Filter.Eq(o => o.Id, order.Id), order, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        await Collection.DeleteOneAsync(Filter.Eq(o => o.Id, id), cancellationToken: ct);
    }
}
