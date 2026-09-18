using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IAuditLogRepository
{
    Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default);
    Task<Page<AuditLogEntry>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<Page<AuditLogEntry>> ListForEntityAsync(
        string entityType, string entityId, int pageNumber, int pageSize, CancellationToken ct = default);
}
