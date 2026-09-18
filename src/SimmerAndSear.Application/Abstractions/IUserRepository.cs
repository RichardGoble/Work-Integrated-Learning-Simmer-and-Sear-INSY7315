using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Abstractions;
public interface IUserRepository
{
    Task<User?> GetAsync(string id, CancellationToken ct = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken ct = default);

    Task<Page<User>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default);

    Task<bool> AnyAdminExistsAsync(CancellationToken ct = default);

    Task<string> CreateAsync(User user, CancellationToken ct = default);

    Task UpdateAsync(User user, CancellationToken ct = default);
    Task RecordFailedLoginAsync(string userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken ct = default);

    Task RecordSuccessfulLoginAsync(string userId, CancellationToken ct = default);
}
