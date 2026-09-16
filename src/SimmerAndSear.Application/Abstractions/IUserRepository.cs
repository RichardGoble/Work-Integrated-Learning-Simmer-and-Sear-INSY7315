using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;

/// <summary>Repository for accounts. Identity is owned by this application.</summary>
public interface IUserRepository
{
    Task<User?> GetAsync(string id, CancellationToken ct = default);

    /// <summary>
    /// Case-insensitive lookup. Backed by a collated unique index, so
    /// "Bob@example.com" and "bob@example.com" cannot both exist.
    /// </summary>
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<Page<User>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<bool> AnyAdminExistsAsync(CancellationToken ct = default);

    Task<string> CreateAsync(User user, CancellationToken ct = default);

    Task UpdateAsync(User user, CancellationToken ct = default);

    /// <summary>Increments the lockout counter without rewriting the whole document.</summary>
    Task RecordFailedLoginAsync(string userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken ct = default);

    Task RecordSuccessfulLoginAsync(string userId, CancellationToken ct = default);
}
