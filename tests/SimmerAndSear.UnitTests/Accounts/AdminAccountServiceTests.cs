using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Accounts;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Security;

namespace SimmerAndSear.UnitTests.Accounts;

//References
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 21 September 2026].

//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 21 September 2026].

public sealed class AdminAccountServiceTests
{
    private const string Password = "a-long-admin-password";

    private readonly MemoryUserRepository _users = new();

    private AdminAccountService CreateService() => new(_users, new PasswordHasher());

    [Fact]
    public async Task Create_makes_an_active_admin_with_a_hashed_password()
    {
        var request = new CreateAdminAccountRequest
        {
            Email = "  Sales@Example.com ", Password = Password, FirstName = "Sam", LastName = "Sales"
        };

        var result = await CreateService().CreateAsync(request, RoleName.SalesAdmin, CancellationToken.None);

        result.Outcome.ShouldBe(AdminAccountOutcome.Ok);
        var stored = _users.Items.ShouldHaveSingleItem();
        stored.Email.ShouldBe("sales@example.com");
        stored.UserType.ShouldBe(UserType.Admin);
        stored.IsActive.ShouldBeTrue();
        stored.Role.ShouldBe(RoleName.SalesAdmin);
        stored.PasswordHash.ShouldNotBe(Password);
    }

    [Fact]
    public async Task Admin_cannot_deactivate_their_own_account()
    {
        var admin = new User { Id = "admin-01", Email = "master@example.com", UserType = UserType.Admin, Role = RoleName.MasterAdmin };
        _users.Items.Add(admin);

        var result = await CreateService().SetActiveAsync(admin.Id, false, admin.Id, CancellationToken.None);

        result.Outcome.ShouldBe(AdminAccountOutcome.CannotDeactivateSelf);
        admin.IsActive.ShouldBeTrue();
    }

    private sealed class MemoryUserRepository : IUserRepository
    {
        public List<User> Items { get; } = [];

        public Task<User?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(u => u.Id == id));

        public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));

        public Task<Page<User>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new Page<User>(Items, Items.Count, pageNumber, pageSize));

        public Task<Page<User>> ListAdminsAsync(int pageNumber, int pageSize, CancellationToken ct = default)
        {
            var admins = Items.Where(u => u.UserType == UserType.Admin).ToList();
            return Task.FromResult(new Page<User>(admins, admins.Count, pageNumber, pageSize));
        }

        public Task<bool> AnyAdminExistsAsync(CancellationToken ct = default) =>
            Task.FromResult(Items.Any(u => u.UserType == UserType.Admin));

        public Task<string> CreateAsync(User user, CancellationToken ct = default)
        {
            user.Id = Guid.NewGuid().ToString("N");
            Items.Add(user);
            return Task.FromResult(user.Id);
        }

        public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;

        public Task RecordFailedLoginAsync(string userId, int lockoutThreshold, TimeSpan lockoutDuration, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task RecordSuccessfulLoginAsync(string userId, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken ct = default)
        {
            Items.RemoveAll(u => u.Id == id);
            return Task.CompletedTask;
        }
    }
}
