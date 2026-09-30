using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Accounts;

// ── References ──────────────────────────────────────────────────────────────
//OWASP Foundation, n.d. Authentication - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class AccountService(IUserRepository users, IPasswordHasher hasher, LoginTiming timing)
{

    public async Task<User?> LoginAsync(LoginRequest request, CancellationToken ct)
    {
        var user = await users.GetByEmailAsync(request.Email.Trim(), ct);
        var valid = hasher.Verify(user?.PasswordHash ?? timing.DummyHash, request.Password);
        if (user is null || !user.IsActive || user.IsLockedOut(DateTime.UtcNow)) return null;
        if (!valid)
        {
            await users.RecordFailedLoginAsync(user.Id, 5, TimeSpan.FromMinutes(15), ct);
            return null;
        }
        await users.RecordSuccessfulLoginAsync(user.Id, ct);
        return await users.GetAsync(user.Id, ct) is { IsActive: true, FailedLoginAttempts: < 5 } current &&
            !current.IsLockedOut(DateTime.UtcNow) && current.PasswordHash == user.PasswordHash
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

    public async Task<string?> ChangeEmailAsync(string userId, string newEmail, CancellationToken ct)
    {
        var email = newEmail.Trim().ToLowerInvariant();

        if (email.Length is < 3 or > 254 || !email.Contains('@'))
        {
            return "Enter a valid email address.";
        }

        var user = await users.GetAsync(userId, ct);

        if (user is null)
        {
            return "We could not find your account.";
        }

        if (string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        if (await users.GetByEmailAsync(email, ct) is not null)
        {
            return "That email address is already in use.";
        }

        user.Email = email;

        try
        {
            await users.UpdateAsync(user, ct);
        }
        catch (DuplicateEmailException)
        {
            return "That email address is already in use.";
        }

        return null;
    }

    public async Task<string?> ChangePasswordAsync(
        string userId, string currentPassword, string newPassword, CancellationToken ct)
    {
        if (newPassword.Length is < 12 or > 128)
        {
            return "Use a new password of at least 12 characters.";
        }

        var user = await users.GetAsync(userId, ct);

        if (user is null)
        {
            return "We could not find your account.";
        }

        if (hasher.Verify(user.PasswordHash, currentPassword) == false)
        {
            return "Your current password is not correct.";
        }

        user.PasswordHash = hasher.Hash(newPassword);
        await users.UpdateAsync(user, ct);

        return null;
    }

    public async Task<string?> DeactivateAsync(string userId, CancellationToken ct)
    {
        var user = await users.GetAsync(userId, ct);

        if (user is null)
        {
            return "We could not find your account.";
        }

        user.IsActive = false;
        await users.UpdateAsync(user, ct);

        return null;
    }

}
