using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.IntegrationTests;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//MongoDB, n.d. CRUD Operations [Source code].
//Available at: <https://www.mongodb.com/docs/drivers/csharp/current/crud/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class LoginPersistenceTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private static User NewUser() => new()
    {
        Email = $"login-{Guid.NewGuid():N}@example.com", FirstName = "Test", LastName = "Account",
        PasswordHash = "test-hash", Client = new ClientProfile()
    };

    [Fact]
    public async Task Concurrent_failures_lock_account_without_extending_the_lock()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = NewUser();
        await users.CreateAsync(user);
        await Task.WhenAll(Enumerable.Range(0, 10).Select(_ =>
            users.RecordFailedLoginAsync(user.Id, 5, TimeSpan.FromMinutes(15))));
        var locked = (await users.GetAsync(user.Id))!;
        locked.FailedLoginAttempts.ShouldBe(5);
        locked.IsLockedOut(DateTime.UtcNow).ShouldBeTrue();
        await users.RecordSuccessfulLoginAsync(user.Id);
        (await users.GetAsync(user.Id))!.LockedOutUntil.ShouldBe(locked.LockedOutUntil);
    }

    [Fact]
    public async Task Expired_lockout_starts_a_fresh_failure_count()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = NewUser();
        user.FailedLoginAttempts = 5;
        user.LockedOutUntil = DateTime.UtcNow.AddMinutes(-1);
        await users.CreateAsync(user);
        await users.RecordFailedLoginAsync(user.Id, 5, TimeSpan.FromMinutes(15));
        var result = (await users.GetAsync(user.Id))!;
        result.FailedLoginAttempts.ShouldBe(1);
        result.LockedOutUntil.ShouldBeNull();
        await users.RecordSuccessfulLoginAsync(user.Id);
        (await users.GetAsync(user.Id))!.FailedLoginAttempts.ShouldBe(0);
    }

    [Fact]
    public async Task Duplicate_email_is_rejected_even_with_different_casing()
    {
        using var scope = factory.Services.CreateScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var user = NewUser();
        await users.CreateAsync(user);
        var duplicate = NewUser();
        duplicate.Email = user.Email.ToUpperInvariant();
        await Should.ThrowAsync<DuplicateEmailException>(() => users.CreateAsync(duplicate));
    }
}
