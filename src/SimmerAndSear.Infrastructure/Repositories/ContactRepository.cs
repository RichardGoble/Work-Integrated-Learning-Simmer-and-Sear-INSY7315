using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class ContactRepository(IMongoDatabase database)
    : MongoRepositoryBase<ContactRequest>(database, MongoCollections.ContactRequests), IContactRepository
{
    public async Task<List<ContactRequest>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Filter.Empty).Sort(SortBy.Descending(c => c.SubmittedAt)).ToListAsync(ct);

    public async Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default)
    {
        request.SubmittedAt = DateTime.UtcNow;
        await Collection.InsertOneAsync(request, cancellationToken: ct);
        return request.Id;
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        await Collection.DeleteOneAsync(Filter.Eq(c => c.Id, id), cancellationToken: ct);
    }
}
