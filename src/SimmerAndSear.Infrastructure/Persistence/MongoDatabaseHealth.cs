using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Persistence;

/// <summary>
/// Backs the readiness probe. Kept behind <see cref="IDatabaseHealth"/> so the
/// web layer can report on the database without naming a MongoDB type.
/// </summary>
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
            // Logged in full here; the endpoint returns nothing but a status, so
            // that connection details never reach a public response.
            logger.LogError(ex, "Readiness probe failed: database unreachable.");
            return false;
        }
    }
}
