using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

/// <summary>
/// Append-only by construction. There is deliberately no Update and no Delete on
/// this interface: the guarantee that audit history cannot be rewritten is made
/// here, in the repository contract, and again by the collection's validator -
/// not left to the discipline of whoever writes the next feature.
/// </summary>
public interface IAuditLogRepository
{
    Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default);

    /// <summary>Readable only by the Master Admin; the policy is enforced at the endpoint.</summary>
    Task<Page<AuditLogEntry>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<AuditLogEntry>> ListForEntityAsync(
        string entityType, string entityId, int pageNumber, int pageSize, CancellationToken ct = default);
}
