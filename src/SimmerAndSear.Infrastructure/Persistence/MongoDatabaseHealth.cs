using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Persistence;
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
