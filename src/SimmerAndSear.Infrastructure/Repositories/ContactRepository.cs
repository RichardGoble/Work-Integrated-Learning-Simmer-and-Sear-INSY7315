using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

public sealed class ContactRepository(IMongoDatabase database)
    : MongoRepositoryBase<ContactRequest>(database, MongoCollections.ContactRequests), IContactRepository
{
    public async Task<ContactRequest?> GetAsync(string id, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return null;
        return await Collection.Find(Filter.Eq(c => c.Id, id)).FirstOrDefaultAsync(ct);
    }

    public async Task<List<ContactRequest>> ListAsync(CancellationToken ct = default) =>
        await Collection.Find(Filter.Empty).Sort(SortBy.Descending(c => c.SubmittedAt)).ToListAsync(ct);

    public async Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default)
    {
        if (request.SubmittedAt == default) request.SubmittedAt = DateTime.UtcNow;
        if (request.UpdatedAt == default) request.UpdatedAt = request.SubmittedAt;
        await Collection.InsertOneAsync(request, cancellationToken: ct);
        return request.Id;
    }

    public async Task UpdateStatusAsync(string id, SimmerAndSear.Domain.Enums.ContactStatus status, string handledBy, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        var update = Update
            .Set(c => c.Status, status)
            .Set(c => c.HandledBy, handledBy)
            .Set(c => c.UpdatedAt, DateTime.UtcNow);
        await Collection.UpdateOneAsync(Filter.Eq(c => c.Id, id), update, cancellationToken: ct);
    }

    public async Task UpdateNotificationStateAsync(
        string id,
        SimmerAndSear.Domain.Enums.ContactNotificationStatus status,
        string? error,
        DateTime? sentAt,
        CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        var update = Update
            .Set(c => c.NotificationStatus, status)
            .Set(c => c.NotificationError, error)
            .Set(c => c.NotificationSentAt, sentAt);
        await Collection.UpdateOneAsync(Filter.Eq(c => c.Id, id), update, cancellationToken: ct);
    }

    public async Task DeleteAsync(string id, CancellationToken ct = default)
    {
        if (!IsValidId(id)) return;
        await Collection.DeleteOneAsync(Filter.Eq(c => c.Id, id), cancellationToken: ct);
    }
}
