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
    Task DeleteAsync(string id, CancellationToken ct = default);
}
