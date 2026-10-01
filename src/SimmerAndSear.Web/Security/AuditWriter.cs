using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

// ── References ──────────────────────────────────────────────────────────────
//OWASP Foundation, n.d. Logging - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class AuditWriter(IAuditLogRepository repository, CurrentUser current,
    IHttpContextAccessor accessor, ILogger<AuditWriter> logger)
{
    public async Task WriteAsync(AuditAction action, string summary, User? actor = null)
    {
        var context = accessor.HttpContext;
        var entry = AuditLogEntry.Create(actor?.Id ?? current.Id ?? "anonymous",
            actor?.FullName ?? current.Name, actor?.Role ?? current.Role ?? RoleName.Customer,
            action, "Account", actor?.Id ?? current.Id, summary, context?.Connection.RemoteIpAddress?.ToString());
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await repository.AppendAsync(entry, timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit persistence failed for {Action}, actor {ActorId}", action, entry.ActorId);
        }
        logger.LogInformation("{SecurityEvent}: actor {ActorId}; {Summary}",
            action == AuditAction.AccessDenied ? "ACCESS_DENIED" : action.ToString(), entry.ActorId, summary);
    }

    public async Task WriteAsync(AuditAction action, string entityType, string? entityId, string summary)
    {
        var context = accessor.HttpContext;
        var entry = AuditLogEntry.Create(current.Id ?? "anonymous", current.Name,
            current.Role ?? RoleName.Customer, action, entityType, entityId, summary,
            context?.Connection.RemoteIpAddress?.ToString());
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await repository.AppendAsync(entry, timeout.Token);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Audit persistence failed for {Action}, actor {ActorId}", action, entry.ActorId);
        }
        logger.LogInformation("{SecurityEvent}: actor {ActorId}; {Summary}",
            action == AuditAction.AccessDenied ? "ACCESS_DENIED" : action.ToString(), entry.ActorId, summary);
    }
}
