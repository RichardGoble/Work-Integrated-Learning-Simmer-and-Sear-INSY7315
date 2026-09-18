using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Persistence;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. Run a Database Command [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/run-command/> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MongoDatabaseHealth(IMongoDatabase database, ILogger<MongoDatabaseHealth> logger)
    : IDatabaseHealth
{
    public async Task<bool> IsReachableAsync(CancellationToken ct = default)
    {
        try
        {
            await database.RunCommandAsync<BsonDocument>(
                new BsonDocument("ping", 1), cancellationToken: ct);

            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Readiness probe failed: database unreachable.");
            return false;
        }
    }
}
