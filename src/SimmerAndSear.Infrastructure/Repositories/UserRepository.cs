using MongoDB.Driver;
using MongoDB.Bson;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;
// ── References ──────────────────────────────────────────────────────────────
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
//
//OWASP Foundation, n.d. Authentication - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class UserRepository(IMongoDatabase database)
    : MongoRepositoryBase<User>(database, MongoCollections.Users), IUserRepository
{
    public async Task<User?> GetAsync(string id, CancellationToken ct = default) =>
        await Collection.Find(Filter.Eq(u => u.Id, id)).Limit(1).FirstOrDefaultAsync(ct);

    public async Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
        await Collection
            .Find(Filter.Eq(u => u.Email, email), new FindOptions { Collation = CaseInsensitive })
            .Limit(1)
            .FirstOrDefaultAsync(ct);

    public Task<Page<User>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        PageAsync(Filter.Empty, SortBy.Ascending(u => u.Email), pageNumber, pageSize, ct: ct);

    public async Task<bool> AnyAdminExistsAsync(CancellationToken ct = default) =>
        await Collection
            .Find(Filter.Eq(u => u.UserType, UserType.Admin))
            .Limit(1)
            .AnyAsync(ct);

    public async Task<string> CreateAsync(User user, CancellationToken ct = default)
    {
        user.CreatedAt = DateTime.UtcNow;
        try { await Collection.InsertOneAsync(user, cancellationToken: ct); }
        catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            throw new DuplicateEmailException();
        }
        return user.Id;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default) =>
        await Collection.ReplaceOneAsync(Filter.Eq(u => u.Id, user.Id), user, cancellationToken: ct);
    public async Task RecordFailedLoginAsync(
        string userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var expired = new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$ne", new BsonArray { "$lockedOutUntil", BsonNull.Value }),
            new BsonDocument("$lte", new BsonArray { "$lockedOutUntil", now })
        });
        PipelineDefinition<User, User> pipeline = new BsonDocument[]
        {
            new("$set", new BsonDocument("failedLoginAttempts", new BsonDocument("$add", new BsonArray
            {
                new BsonDocument("$cond", new BsonArray { expired, 0,
                    new BsonDocument("$ifNull", new BsonArray { "$failedLoginAttempts", 0 }) }), 1
            }))),
            new("$set", new BsonDocument("lockedOutUntil", new BsonDocument("$cond", new BsonArray
            {
                new BsonDocument("$gte", new BsonArray { "$failedLoginAttempts", lockoutThreshold }),
                now.Add(lockoutDuration), BsonNull.Value
            })))
        };
        await Collection.UpdateOneAsync(
            Filter.Eq(u => u.Id, userId) & Filter.Eq(u => u.IsActive, true) &
            (Filter.Eq(u => u.LockedOutUntil, null) | Filter.Lte(u => u.LockedOutUntil, now)),
            Update.Pipeline(pipeline), cancellationToken: ct);
    }
    public async Task RecordSuccessfulLoginAsync(string userId, CancellationToken ct = default) =>
        await Collection.UpdateOneAsync(
            Filter.Eq(u => u.Id, userId) & Filter.Eq(u => u.IsActive, true) &
            (Filter.Eq(u => u.LockedOutUntil, null) | Filter.Lte(u => u.LockedOutUntil, DateTime.UtcNow)),
            Update.Set(u => u.FailedLoginAttempts, 0)
                  .Set(u => u.LockedOutUntil, null)
                  .Set(u => u.LastLogin, DateTime.UtcNow),
            cancellationToken: ct);
}
