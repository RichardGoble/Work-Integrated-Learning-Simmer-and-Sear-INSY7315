using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Accounts;

public sealed class AccountService(IUserRepository users, IPasswordHasher hasher)
{
    private static readonly Lazy<string> DummyPassword = new(() => Guid.NewGuid().ToString());
    private string DummyHash => hasher.Hash(DummyPassword.Value);

    public async Task<User?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim(), ct);
        var valid = hasher.Verify(user?.PasswordHash ?? DummyHash, request.Password);
        if (user is null || !user.IsActive || user.IsLockedOut(DateTime.UtcNow)) return null;
        if (!valid)
        {
            await users.RecordFailedLoginAsync(user.Id, 5, TimeSpan.FromMinutes(15), ct);
            return null;
        }
        await users.RecordSuccessfulLoginAsync(user.Id, ct);
        return await users.GetAsync(user.Id, ct) is { IsActive: true, FailedLoginAttempts: < 5 } current && !current.IsLockedOut(DateTime.UtcNow)
            ? current : null;
    }

    public async Task<User?> RegisterAsync(RegisterRequest request, CancellationToken ct)
    {
        if (request.Password.Length is < 12 or > 128) return null;
        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, ct) is not null) return null;
        var user = new User
        {
            Email = email, PasswordHash = hasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Role = RoleName.Customer, UserType = UserType.Client, Client = new ClientProfile()
        };
        try { await users.CreateAsync(user, ct); }
        catch (DuplicateEmailException) { return null; }
        return user;
    }
}
