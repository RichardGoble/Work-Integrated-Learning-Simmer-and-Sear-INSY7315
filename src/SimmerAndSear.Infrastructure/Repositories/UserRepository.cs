using MongoDB.Driver;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Persistence;

namespace SimmerAndSear.Infrastructure.Repositories;

/// <summary>Repository for accounts.</summary>
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
        await Collection.InsertOneAsync(user, cancellationToken: ct);
        return user.Id;
    }

    public async Task UpdateAsync(User user, CancellationToken ct = default) =>
        await Collection.ReplaceOneAsync(Filter.Eq(u => u.Id, user.Id), user, cancellationToken: ct);

    /// <summary>
    /// Increments the failure counter atomically and applies a lockout once the
    /// threshold is crossed. Together with the rate limiter on the login route
    /// this is the "rate-limiting on failed logins" NFR.
    /// </summary>
    public async Task RecordFailedLoginAsync(
        string userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken ct = default)
    {
        var updated = await Collection.FindOneAndUpdateAsync(
            Filter.Eq(u => u.Id, userId),
            Update.Inc(u => u.FailedLoginAttempts, 1),
            new FindOneAndUpdateOptions<User> { ReturnDocument = ReturnDocument.After },
            ct);

        if (updated is not null && updated.FailedLoginAttempts >= lockoutThreshold)
        {
            await Collection.UpdateOneAsync(
                Filter.Eq(u => u.Id, userId),
                Update.Set(u => u.LockedOutUntil, DateTime.UtcNow.Add(lockoutDuration)),
                cancellationToken: ct);
        }
    }

    public async Task RecordSuccessfulLoginAsync(string userId, CancellationToken ct = default) =>
        await Collection.UpdateOneAsync(
            Filter.Eq(u => u.Id, userId),
            Update.Set(u => u.FailedLoginAttempts, 0)
                  .Set(u => u.LockedOutUntil, null)
                  .Set(u => u.LastLogin, DateTime.UtcNow),
            cancellationToken: ct);
}
