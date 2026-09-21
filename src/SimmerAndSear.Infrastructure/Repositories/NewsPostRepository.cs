using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class NewsPostRepository(IMongoDatabase database)
    : MongoRepositoryBase<NewsPost>(database, MongoCollections.NewsPosts), INewsPostRepository
{
    public async Task<NewsPost?> GetAsync(string id, CancellationToken ct = default) =>
        !IsValidId(id)
            ? null
            : await Collection.Find(Filter.Eq(p => p.Id, id)).Limit(1).FirstOrDefaultAsync(ct);

    public Task<Page<NewsPost>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Empty, SortBy.Descending(p => p.CreatedAt), pageNumber, pageSize, ct: ct);

    public Task<Page<NewsPost>> ListByStatusAsync(PostStatus status, int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Eq(p => p.Status, status), SortBy.Descending(p => p.CreatedAt), pageNumber, pageSize, ct: ct);

    public async Task<string> CreateAsync(NewsPost post, CancellationToken ct = default)
    {
        post.CreatedAt = DateTime.UtcNow;
        post.UpdatedAt = post.CreatedAt;
        await Collection.InsertOneAsync(post, cancellationToken: ct);
        return post.Id;
    }

    public async Task UpdateAsync(NewsPost post, CancellationToken ct = default)
    {
        post.UpdatedAt = DateTime.UtcNow;
        await Collection.ReplaceOneAsync(Filter.Eq(p => p.Id, post.Id), post, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default) =>
        await Collection.DeleteOneAsync(Filter.Eq(p => p.Id, id), ct);
}
