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

//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].
public sealed class SecurityFactory : WebApplicationFactory<Program>
{
    public MemoryUsers Users { get; } = new();
    public MemoryAudit Audit { get; } = new();
    public MemoryOrders Orders { get; } = new();
    public MemoryWishlists Wishlists { get; } = new();
    public MemoryQuotationDocuments Quotations { get; } = new();
    public MemoryFileStore Files { get; } = new();
    public MemoryContacts Contacts { get; } = new();
    public MemoryBookings Bookings { get; } = new();
    public MemoryReviews Reviews { get; } = new();

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
            services.RemoveAll<IQuotationDocumentRepository>();
            services.RemoveAll<IFileStore>();
            services.AddSingleton<IQuotationDocumentRepository>(Quotations);
            services.AddSingleton<IFileStore>(Files);
            services.RemoveAll<IContactRepository>();
            services.RemoveAll<IBookingRepository>();
            services.RemoveAll<IReviewRepository>();
            services.RemoveAll<SimmerAndSear.Application.Engagement.ITransactionalEmailSender>();
            services.AddSingleton<IContactRepository>(Contacts);
            services.AddSingleton<IBookingRepository>(Bookings);
            services.AddSingleton<IReviewRepository>(Reviews);
            services.AddSingleton<SimmerAndSear.Application.Engagement.ITransactionalEmailSender, FakeEmailSender>();
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
    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Items.RemoveAll(u => u.Id == id);
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
    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Items.RemoveAll(o => o.Id == id);
        return Task.CompletedTask;
    }
    public Task<bool> HasDeliveredOrderWithProductAsync(string clientId, string productId, CancellationToken ct = default) =>
        Task.FromResult(Items.Any(o =>
            o.ClientId == clientId &&
            o.Status == OrderStatus.Delivered &&
            o.Items.Any(i => i.ProductId == productId)));
}

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

public sealed class FixedSiteSettings : ISiteSettingsProvider
{
    private readonly SiteSettings _settings = new() { SalesNotificationEmail = "sales@example.com" };
    public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) => Task.FromResult(_settings);
    public void Invalidate()
    {
    }
}

public sealed class MemoryQuotationDocuments : IQuotationDocumentRepository
{
    public List<QuotationDocument> Items { get; } = [];
    public Task<QuotationDocument?> GetByOrderAsync(string orderId, CancellationToken ct = default) =>
        Task.FromResult(Items.Find(d => d.OrderId == orderId));
    public Task<bool> TryCreateAsync(QuotationDocument document, CancellationToken ct = default)
    {
        if (Items.Any(d => d.OrderId == document.OrderId)) return Task.FromResult(false);
        document.Id = Guid.NewGuid().ToString("N");
        Items.Add(document);
        return Task.FromResult(true);
    }
}

public sealed class MemoryFileStore : IFileStore
{
    public Dictionary<string, byte[]> Items { get; } = [];
    public async Task<string> UploadAsync(Stream content, string fileName, string contentType, CancellationToken ct = default)
    {
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, ct);
        var id = Guid.NewGuid().ToString("N");
        Items[id] = buffer.ToArray();
        return id;
    }
    public Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default) =>
        Task.FromResult<Stream>(new MemoryStream(Items[fileId], writable: false));
    public Task DeleteAsync(string fileId, CancellationToken ct = default)
    {
        Items.Remove(fileId);
        return Task.CompletedTask;
    }
}

public sealed class MemoryContacts : IContactRepository
{
    public List<ContactRequest> Items { get; } = [];

    public Task<ContactRequest?> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Items.Find(c => c.Id == id));

    public Task<List<ContactRequest>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(Items.OrderByDescending(c => c.SubmittedAt).ToList());

    public Task<string> CreateAsync(ContactRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(request.Id)) request.Id = Guid.NewGuid().ToString("N");
        Items.Add(request);
        return Task.FromResult(request.Id);
    }

    public Task UpdateStatusAsync(string id, ContactStatus status, string handledBy, CancellationToken ct = default)
    {
        var item = Items.Find(c => c.Id == id);
        if (item is not null)
        {
            item.Status = status;
            item.HandledBy = handledBy;
            item.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }

    public Task UpdateNotificationStateAsync(
        string id, ContactNotificationStatus status, string? error, DateTime? sentAt, CancellationToken ct = default)
    {
        var item = Items.Find(c => c.Id == id);
        if (item is not null)
        {
            item.NotificationStatus = status;
            item.NotificationError = error;
            item.NotificationSentAt = sentAt;
        }
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Items.RemoveAll(c => c.Id == id);
        return Task.CompletedTask;
    }
}

public sealed class MemoryBookings : IBookingRepository
{
    public List<Booking> Items { get; } = [];

    public Task<Booking?> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Items.Find(b => b.Id == id));

    public Task<List<Booking>> ListAsync(CancellationToken ct = default) =>
        Task.FromResult(Items.OrderByDescending(b => b.CreatedAt).ToList());

    public Task<List<Booking>> ListForClientAsync(string clientId, CancellationToken ct = default) =>
        Task.FromResult(Items.Where(b => b.ClientId == clientId).OrderByDescending(b => b.CreatedAt).ToList());

    public Task<string> CreateAsync(Booking booking, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(booking.Id)) booking.Id = Guid.NewGuid().ToString("N");
        Items.Add(booking);
        return Task.FromResult(booking.Id);
    }

    public Task UpdateStatusAsync(string id, BookingStatus status, CancellationToken ct = default)
    {
        var item = Items.Find(b => b.Id == id);
        if (item is not null)
        {
            item.Status = status;
            item.UpdatedAt = DateTime.UtcNow;
        }
        return Task.CompletedTask;
    }
}

public sealed class FakeEmailSender : SimmerAndSear.Application.Engagement.ITransactionalEmailSender
{
    public List<(string Email, string Name, string Subject, string Text)> SentEmails { get; } = [];

    public Task<bool> SendEmailAsync(
        string recipientEmail, string recipientName, string subject, string textContent, CancellationToken ct = default)
    {
        SentEmails.Add((recipientEmail, recipientName, subject, textContent));
        return Task.FromResult(true);
    }
}

public sealed class MemoryReviews : IReviewRepository
{
    public List<Review> Items { get; } = [];

    public Task<Page<Review>> ListForClientAsync(string clientId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var mine = Items.Where(r => r.ClientId == clientId).ToList();
        return Task.FromResult(new Page<Review>(mine, mine.Count, pageNumber, pageSize));
    }

    public Task<Review?> GetAsync(string id, CancellationToken ct = default) =>
        Task.FromResult(Items.Find(r => r.Id == id));

    public Task<Page<Review>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
        Task.FromResult(new Page<Review>(Items, Items.Count, pageNumber, pageSize));

    public Task<Page<Review>> ListByStatusAsync(ReviewStatus status, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var filtered = Items.Where(r => r.Status == status).ToList();
        return Task.FromResult(new Page<Review>(filtered, filtered.Count, pageNumber, pageSize));
    }

    public Task<Page<Review>> ListForProductAsync(string productId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var filtered = Items.Where(r => r.ProductId == productId && r.Status == ReviewStatus.Approved).ToList();
        return Task.FromResult(new Page<Review>(filtered, filtered.Count, pageNumber, pageSize));
    }

    public Task<bool> HasCompletedOrderForProductAsync(string clientId, string productId, CancellationToken ct = default) =>
        Task.FromResult(true);

    public Task<bool> HasExistingReviewAsync(string clientId, string productId, CancellationToken ct = default) =>
        Task.FromResult(false);

    public Task<string> CreateAsync(Review review, CancellationToken ct = default)
    {
        review.Id = Guid.NewGuid().ToString("N");
        Items.Add(review);
        return Task.FromResult(review.Id);
    }

    public Task UpdateAsync(Review review, CancellationToken ct = default) => Task.CompletedTask;

    public Task DeleteAsync(string id, CancellationToken ct = default)
    {
        Items.RemoveAll(r => r.Id == id);
        return Task.CompletedTask;
    }
}

