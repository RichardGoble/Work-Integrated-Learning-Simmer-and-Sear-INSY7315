using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
public interface IAuditLogRepository
{
    Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<Page<AuditLogEntry>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<AuditLogEntry>> ListForEntityAsync(
        string entityType, string entityId, int pageNumber, int pageSize, CancellationToken ct = default);
}
