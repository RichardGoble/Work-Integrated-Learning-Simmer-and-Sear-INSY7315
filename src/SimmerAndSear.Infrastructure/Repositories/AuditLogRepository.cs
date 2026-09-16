using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

/// <summary>
/// Append-only store for the audit trail.
/// <para>
/// There is no update and no delete here, and none may be added. The collection's
/// $jsonSchema validator is the second layer, and the database user's privileges
/// are the third. An audit log that the application can rewrite is not an audit log.
/// </para>
/// </summary>
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
