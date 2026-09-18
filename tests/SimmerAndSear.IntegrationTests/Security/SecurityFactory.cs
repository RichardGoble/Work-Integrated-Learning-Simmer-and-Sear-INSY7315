using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.IntegrationTests.Security;

public sealed class SecurityFactory : WebApplicationFactory<Program>
{
    public MemoryUsers Users { get; } = new();
    public MemoryAudit Audit { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
            new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] = "mongodb://localhost:27017",
                ["Mongo:Database"] = "security_tests",
                ["Mongo:ApplySchemaOnStartup"] = "false"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IUserRepository>();
            services.RemoveAll<IAuditLogRepository>();
            services.AddSingleton<IUserRepository>(Users);
            services.AddSingleton<IAuditLogRepository>(Audit);
            services.AddSingleton<IStartupFilter, RemoteAddressFilter>();
        });
    }

    public HttpClient Browser() => CreateClient(new WebApplicationFactoryClientOptions
        { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });

    public User AddUser(RoleName role = RoleName.Customer)
    {
        var user = new User
        {
            Id = Guid.NewGuid().ToString("N"), Email = $"{Guid.NewGuid():N}@example.com",
            FirstName = "Test", LastName = "User", Role = role,
            UserType = role == RoleName.Customer ? UserType.Client : UserType.Admin,
            PasswordHash = Services.GetRequiredService<IPasswordHasher>().Hash(SecurityTests.Password)
        };
        Users.Items.Add(user);
        return user;
    }
}

public sealed class RemoteAddressFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) => app =>
    {
        app.Use(async (context, following) =>
        {
            context.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("198.51.100.1");
            await following();
        });
        next(app);
    };
}

public sealed class MemoryUsers : IUserRepository
{
    public List<User> Items { get; } = [];
    public bool ThrowOnRead { get; set; }
    public Task<User?> GetAsync(string id, CancellationToken ct = default) => Task.FromResult(Items.Find(u => u.Id == id));
    public Task<User?> GetByEmailAsync(string email, CancellationToken ct = default)
    {
        if (ThrowOnRead) throw new InvalidOperationException("private database credentials");
        return Task.FromResult(Items.Find(u => u.Email.Equals(email, StringComparison.OrdinalIgnoreCase)));
    }
    public Task<Page<User>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<User>(Items, Items.Count, pageNumber, pageSize));
    public Task<bool> AnyAdminExistsAsync(CancellationToken ct = default) => Task.FromResult(Items.Any(u => u.UserType == UserType.Admin));
    public Task<string> CreateAsync(User user, CancellationToken ct = default)
    {
        if (Items.Any(u => u.Email.Equals(user.Email, StringComparison.OrdinalIgnoreCase))) throw new DuplicateEmailException();
        user.Id = Guid.NewGuid().ToString("N");
        Items.Add(user);
        return Task.FromResult(user.Id);
    }
    public Task UpdateAsync(User user, CancellationToken ct = default) => Task.CompletedTask;
    public Task RecordFailedLoginAsync(string userId, int threshold, TimeSpan duration, CancellationToken ct = default)
    {
        var user = Items.Single(u => u.Id == userId);
        if (user.IsLockedOut(DateTime.UtcNow)) return Task.CompletedTask;
        if (user.LockedOutUntil is not null) { user.FailedLoginAttempts = 0; user.LockedOutUntil = null; }
        user.FailedLoginAttempts++;
        if (user.FailedLoginAttempts >= threshold) user.LockedOutUntil = DateTime.UtcNow.Add(duration);
        return Task.CompletedTask;
    }
    public Task RecordSuccessfulLoginAsync(string userId, CancellationToken ct = default)
    {
        var user = Items.Single(u => u.Id == userId);
        if (!user.IsLockedOut(DateTime.UtcNow))
        {
            user.FailedLoginAttempts = 0;
            user.LockedOutUntil = null;
            user.LastLogin = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}

public sealed class MemoryAudit : IAuditLogRepository
{
    public List<AuditLogEntry> Items { get; } = [];
    public bool ThrowOnWrite { get; set; }
    public Task AppendAsync(AuditLogEntry entry, CancellationToken ct = default)
    {
        if (ThrowOnWrite) throw new InvalidOperationException("audit offline");
        Items.Add(entry);
        return Task.CompletedTask;
    }
    public Task<Page<AuditLogEntry>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<AuditLogEntry>(Items, Items.Count, pageNumber, pageSize));
    public Task<Page<AuditLogEntry>> ListForEntityAsync(string entityType, string entityId, int pageNumber, int pageSize, CancellationToken ct = default) =>
        ListAsync(pageNumber, pageSize, ct);
}
