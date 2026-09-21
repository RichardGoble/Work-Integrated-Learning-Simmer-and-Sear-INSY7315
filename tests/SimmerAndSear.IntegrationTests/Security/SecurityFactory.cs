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

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class SecurityFactory : WebApplicationFactory<Program>
{
    public MemoryUsers Users { get; } = new();
    public MemoryAudit Audit { get; } = new();
    public MemoryOrders Orders { get; } = new();
    public MemoryWishlists Wishlists { get; } = new();

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
            services.RemoveAll<IOrderRepository>();
            services.RemoveAll<IWishlistRepository>();
            services.RemoveAll<ISiteSettingsProvider>();
            services.AddSingleton<IOrderRepository>(Orders);
            services.AddSingleton<IWishlistRepository>(Wishlists);
            services.AddSingleton<ISiteSettingsProvider, FixedSiteSettings>();
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

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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
    public Task<Page<User>> ListAdminsAsync(int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var admins = Items.Where(u => u.UserType == UserType.Admin).OrderBy(u => u.Email).ToList();
        return Task.FromResult(new Page<User>(admins, admins.Count, pageNumber, pageSize));
    }
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

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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

//References
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].

public sealed class MemoryOrders : IOrderRepository
{
    public List<Order> Items { get; } = [];
    public Task<Order?> GetAsync(string id, CancellationToken ct = default) => Task.FromResult(Items.Find(o => o.Id == id));
    public Task<Order?> GetForClientAsync(string id, string clientId, CancellationToken ct = default) =>
        Task.FromResult(Items.Find(o => o.Id == id && o.ClientId == clientId));
    public Task<Page<Order>> ListForClientAsync(string clientId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var owned = Items.Where(o => o.ClientId == clientId).ToList();
        return Task.FromResult(new Page<Order>(owned, owned.Count, pageNumber, pageSize));
    }
    public Task<Page<Order>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<Order>(Items, Items.Count, pageNumber, pageSize));
    public Task<string> CreateAsync(Order order, CancellationToken ct = default)
    {
        order.Id = Guid.NewGuid().ToString("N");
        Items.Add(order);
        return Task.FromResult(order.Id);
    }
    public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
}

//References
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].

public sealed class MemoryWishlists : IWishlistRepository
{
    public List<Wishlist> Items { get; } = [];
    public Task<Wishlist?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
        Task.FromResult(Items.Find(w => w.ClientId == clientId));
    public Task SaveAsync(Wishlist wishlist, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(wishlist.Id))
        {
            wishlist.Id = Guid.NewGuid().ToString("N");
            Items.Add(wishlist);
        }
        wishlist.UpdatedAt = DateTime.UtcNow;
        return Task.CompletedTask;
    }
}

//References
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 21 September 2026].

public sealed class FixedSiteSettings : ISiteSettingsProvider
{
    private readonly SiteSettings _settings = new();
    public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) => Task.FromResult(_settings);
    public void Invalidate()
    {
    }
}
