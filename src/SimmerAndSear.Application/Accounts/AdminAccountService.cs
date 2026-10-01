using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Accounts;

//References
//OWASP Foundation, n.d. Mass Assignment - OWASP Cheat Sheet Series [Source code].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html> [Accessed 21 September 2026].

//OWASP Foundation, n.d. Authentication - OWASP Cheat Sheet Series [Source code].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Authentication_Cheat_Sheet.html> [Accessed 21 September 2026].

//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 21 September 2026].

public enum AdminAccountOutcome
{
    Ok,
    NotFound,
    DuplicateEmail,
    CannotDeactivateSelf
}

public sealed record AdminAccountResult(AdminAccountOutcome Outcome, AdminAccountResponse? Account);

public sealed class AdminAccountService(IUserRepository users, IPasswordHasher hasher)
{
    public async Task<Page<AdminAccountResponse>> ListAsync(int pageNumber, int pageSize, CancellationToken ct)
    {
        var page = await users.ListAdminsAsync(pageNumber, pageSize, ct);
        return new Page<AdminAccountResponse>(
            page.Items.Select(ToResponse).ToList(), page.Total, page.PageNumber, page.PageSize);
    }

    public async Task<AdminAccountResponse?> GetAsync(string id, CancellationToken ct) =>
        await FindAdminAsync(id, ct) is { } user ? ToResponse(user) : null;

    public async Task<AdminAccountResult> CreateAsync(CreateAdminAccountRequest request, RoleName role, CancellationToken ct)
    {
        if (role is not (RoleName.SalesAdmin or RoleName.MasterAdmin)) throw new ArgumentOutOfRangeException(nameof(role));

        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, ct) is not null) return new(AdminAccountOutcome.DuplicateEmail, null);

        var user = new User
        {
            Email = email,
            PasswordHash = hasher.Hash(request.Password),
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = Clean(request.Phone),
            Role = role,
            UserType = UserType.Admin,
            IsActive = true
        };
        try { await users.CreateAsync(user, ct); }
        catch (DuplicateEmailException) { return new(AdminAccountOutcome.DuplicateEmail, null); }
        return new(AdminAccountOutcome.Ok, ToResponse(user));
    }

    public async Task<AdminAccountResult> UpdateAsync(string id, UpdateAdminAccountRequest request, CancellationToken ct)
    {
        var user = await FindAdminAsync(id, ct);
        if (user is null) return new(AdminAccountOutcome.NotFound, null);

        var email = request.Email.Trim().ToLowerInvariant();
        if (await users.GetByEmailAsync(email, ct) is { } other && other.Id != user.Id)
        {
            return new(AdminAccountOutcome.DuplicateEmail, null);
        }

        user.Email = email;
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.Phone = Clean(request.Phone);
        try { await users.UpdateAsync(user, ct); }
        catch (DuplicateEmailException) { return new(AdminAccountOutcome.DuplicateEmail, null); }
        return new(AdminAccountOutcome.Ok, ToResponse(user));
    }

    public async Task<AdminAccountResult> SetActiveAsync(string id, bool active, string actorId, CancellationToken ct)
    {
        if (!active && id == actorId) return new(AdminAccountOutcome.CannotDeactivateSelf, null);

        var user = await FindAdminAsync(id, ct);
        if (user is null) return new(AdminAccountOutcome.NotFound, null);

        user.IsActive = active;
        if (active)
        {
            user.FailedLoginAttempts = 0;
            user.LockedOutUntil = null;
        }
        await users.UpdateAsync(user, ct);
        return new(AdminAccountOutcome.Ok, ToResponse(user));
    }

    private async Task<User?> FindAdminAsync(string id, CancellationToken ct) =>
        await users.GetAsync(id, ct) is { UserType: UserType.Admin } user ? user : null;

    private static string? Clean(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static AdminAccountResponse ToResponse(User user) => new(
        user.Id, user.Email, user.FirstName, user.LastName, user.Phone,
        user.Role.ToString(), user.IsActive, user.CreatedAt, user.LastLogin);
}
