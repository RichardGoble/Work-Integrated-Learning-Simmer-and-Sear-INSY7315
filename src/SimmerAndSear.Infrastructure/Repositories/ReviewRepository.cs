using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class ReviewRepository(IMongoDatabase database, IOrderRepository orders)
    : MongoRepositoryBase<Review>(database, MongoCollections.Reviews), IReviewRepository
{
    public async Task<Review?> GetAsync(string id, CancellationToken ct = default) =>
        !IsValidId(id)
            ? null
            : await Collection.Find(Filter.Eq(r => r.Id, id)).Limit(1).FirstOrDefaultAsync(ct);

    public Task<Page<Review>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Empty, SortBy.Descending(r => r.CreatedAt), pageNumber, pageSize, ct: ct);

    public Task<Page<Review>> ListByStatusAsync(ReviewStatus status, int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Eq(r => r.Status, status), SortBy.Descending(r => r.CreatedAt), pageNumber, pageSize, ct: ct);

    public Task<Page<Review>> ListForProductAsync(string productId, int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(
            Filter.And(Filter.Eq(r => r.ProductId, productId), Filter.Eq(r => r.Status, ReviewStatus.Approved)),
            SortBy.Descending(r => r.CreatedAt),
            pageNumber,
            pageSize,
            ct: ct);

    public async Task<bool> HasCompletedOrderForProductAsync(string clientId, string productId, CancellationToken ct = default)
    {
        var allOrders = await orders.ListForClientAsync(clientId, 1, 100, ct);
        return allOrders.Items.Any(o =>
            o.Status == Domain.Enums.OrderStatus.Delivered &&
            o.Items.Any(i => i.ProductId == productId));
    }

    public async Task<bool> HasExistingReviewAsync(string clientId, string productId, CancellationToken ct = default)
    {
        var count = await Collection.CountDocumentsAsync(
            Filter.And(
                Filter.Eq(r => r.ClientId, clientId),
                Filter.Eq(r => r.ProductId, productId)),
            cancellationToken: ct);
        return count > 0;
    }

    public async Task<string> CreateAsync(Review review, CancellationToken ct = default)
    {
        review.CreatedAt = DateTime.UtcNow;
        review.UpdatedAt = review.CreatedAt;
        await Collection.InsertOneAsync(review, cancellationToken: ct);
        return review.Id;
    }

    public async Task UpdateAsync(Review review, CancellationToken ct = default)
    {
        review.UpdatedAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(Filter.Eq(r => r.Id, review.Id), review, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (IsValidId(id))
        {
            await Collection.DeleteOneAsync(Filter.Eq(r => r.Id, id), ct);
        }
    }
}
