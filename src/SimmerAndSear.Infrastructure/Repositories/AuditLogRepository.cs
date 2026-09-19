using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
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
public sealed class AuditLogRepository(IMongoDatabase database)
    : MongoRepositoryBase<AuditLogEntry>(database, MongoCollections.AuditLog), IAuditLogRepository
{
    public async Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default) =>
        await Collection.InsertOneAsync(entry, cancellationToken: ct);

    public Task<Page<AuditLogEntry>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Empty, SortBy.Descending(a => a.OccurredAt), pageNumber, pageSize, ct: ct);

    public Task<Page<AuditLogEntry>> ListForEntityAsync(
        string entityType, string entityId, int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(
            Filter.And(
                Filter.Eq(a => a.EntityType, entityType),
                Filter.Eq(a => a.EntityId, entityId)),
            SortBy.Descending(a => a.OccurredAt),
            pageNumber,
            pageSize,
            ct: ct);
}
