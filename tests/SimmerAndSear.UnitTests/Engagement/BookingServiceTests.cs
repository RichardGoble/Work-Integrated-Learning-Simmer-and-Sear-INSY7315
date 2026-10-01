using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Contracts.Engagement;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.UnitTests.Engagement;

public sealed class BookingServiceTests
{
    [Fact]
    public async Task CreateAsync_anonymous_booking_without_order_succeeds()
    {
        var bookingRepo = new MemoryBookingRepo();
        var orderRepo = new MemoryOrderRepo();
        var service = new BookingService(bookingRepo, orderRepo);

        var request = new CreateBookingRequest(
            FullName: "Anonymous Chef",
            Email: "chef@example.com",
            Phone: "0823334444",
            PreferredDateTime: DateTime.UtcNow.AddDays(3),
            Purpose: "Commercial cookware consultation",
            OrderId: null
        );

        var (response, error) = await service.CreateAsync(request, null, null, null);

        error.ShouldBeNull();
        response.ShouldNotBeNull();
        response.ClientId.ShouldBeNull();
        response.ClientName.ShouldBe("Anonymous Chef");
        response.ClientEmail.ShouldBe("chef@example.com");
        response.Status.ShouldBe(BookingStatus.Requested.ToString());

        bookingRepo.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_anonymous_booking_cannot_link_an_order()
    {
        var bookingRepo = new MemoryBookingRepo();
        var orderRepo = new MemoryOrderRepo();
        var service = new BookingService(bookingRepo, orderRepo);

        var request = new CreateBookingRequest(
            FullName: "Visitor",
            Email: "vis@example.com",
            Phone: null,
            PreferredDateTime: DateTime.UtcNow.AddDays(2),
            Purpose: "Inquiry",
            OrderId: "order_123"
        );

        var (response, error) = await service.CreateAsync(request, null, null, null);

        response.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("Anonymous bookings cannot be linked to orders");
        bookingRepo.Items.ShouldBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_customer_records_authenticated_id_and_rejects_unowned_order()
    {
        var bookingRepo = new MemoryBookingRepo();
        var orderRepo = new MemoryOrderRepo();
        var service = new BookingService(bookingRepo, orderRepo);

        var request = new CreateBookingRequest(
            FullName: "Spoofed Name",
            Email: "spoofed@example.com",
            Phone: null,
            PreferredDateTime: DateTime.UtcNow.AddDays(5),
            Purpose: "Review custom bulk quotation",
            OrderId: "foreign_order_999"
        );

        // Current customer is cust_123, but foreign_order_999 does not belong to cust_123
        var (response, error) = await service.CreateAsync(
            request,
            currentUserId: "cust_123",
            currentUserName: "Real Customer",
            currentUserEmail: "real@example.com");

        response.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("not belong to your account");
    }

    [Fact]
    public async Task CreateAsync_customer_can_link_owned_order()
    {
        var bookingRepo = new MemoryBookingRepo();
        var orderRepo = new MemoryOrderRepo();
        orderRepo.Items.Add(new Order
        {
            Id = "quote_order_55",
            ClientId = "cust_123",
            OrderType = OrderType.QuoteRequest,
            OrderReference = "QUOTE-55"
        });

        var service = new BookingService(bookingRepo, orderRepo);

        var request = new CreateBookingRequest(
            FullName: null,
            Email: null,
            Phone: null,
            PreferredDateTime: DateTime.UtcNow.AddDays(4),
            Purpose: "Review quotation specifications",
            OrderId: "quote_order_55"
        );

        var (response, error) = await service.CreateAsync(
            request,
            currentUserId: "cust_123",
            currentUserName: "Verified Customer",
            currentUserEmail: "verified@example.com");

        error.ShouldBeNull();
        response.ShouldNotBeNull();
        response.ClientId.ShouldBe("cust_123");
        response.ClientName.ShouldBe("Verified Customer");
        response.OrderId.ShouldBe("quote_order_55");

        bookingRepo.Items.Count.ShouldBe(1);
    }

    [Fact]
    public async Task CreateAsync_past_datetime_is_rejected()
    {
        var bookingRepo = new MemoryBookingRepo();
        var orderRepo = new MemoryOrderRepo();
        var service = new BookingService(bookingRepo, orderRepo);

        var request = new CreateBookingRequest(
            FullName: "Customer",
            Email: "cust@example.com",
            Phone: null,
            PreferredDateTime: DateTime.UtcNow.AddHours(-2), // In the past!
            Purpose: "Consultation",
            OrderId: null
        );

        var (response, error) = await service.CreateAsync(request, null, null, null);

        response.ShouldBeNull();
        error.ShouldNotBeNull();
        error.ShouldContain("must be in the future");
    }

    private sealed class MemoryBookingRepo : IBookingRepository
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

    private sealed class MemoryOrderRepo : IOrderRepository
    {
        public List<Order> Items { get; } = [];

        public Task<Order?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(o => o.Id == id));

        public Task<Order?> GetForClientAsync(string id, string clientId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(o => o.Id == id && o.ClientId == clientId));

        public Task<Page<Order>> ListForClientAsync(string clientId, int pageNumber, int pageSize, CancellationToken ct = default) =>
            Task.FromResult(new Page<Order>(Items.Where(o => o.ClientId == clientId).ToList(), Items.Count, pageNumber, pageSize));

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
}
