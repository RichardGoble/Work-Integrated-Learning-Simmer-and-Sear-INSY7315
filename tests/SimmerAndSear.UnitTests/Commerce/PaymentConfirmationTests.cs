using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Contracts.Catalogue;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.UnitTests.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 25 September 2026].
//
//PayFast, n.d. Instant Transaction Notification [Online guide].
//Available at: <https://developers.payfast.co.za/docs#instant_transaction_notification> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public sealed class PaymentConfirmationTests
{
    private const string Client = "client-1";
    private const string Product = "6ab02de5df59674ee12f998b";

    [Fact]
    public async Task Paying_an_order_takes_the_stock_it_committed_to()
    {
        var (service, products, _) = Build(stock: 10, quantity: 3);

        var result = await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 300m);

        result.Outcome.ShouldBe(PaymentOutcome.Confirmed);
        result.HasStockShortfall.ShouldBeFalse();
        products.Items[0].StockQuantity.ShouldBe(7);
    }

    [Fact]
    public async Task The_same_notification_arriving_twice_does_not_take_the_stock_twice()
    {
        var (service, products, _) = Build(stock: 10, quantity: 3);

        await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 300m);
        var repeat = await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 300m);

        repeat.Outcome.ShouldBe(PaymentOutcome.AlreadyConfirmed);
        products.Items[0].StockQuantity.ShouldBe(7);
    }

    [Fact]
    public async Task A_second_DIFFERENT_payment_on_a_paid_order_is_refused()
    {
        var (service, products, _) = Build(stock: 10, quantity: 3);

        await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 300m);
        var other = await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-002", 300m);

        other.Outcome.ShouldBe(PaymentOutcome.NotAwaitingPayment);
        products.Items[0].StockQuantity.ShouldBe(7);
    }

    [Fact]
    public async Task Paying_the_wrong_amount_is_refused_and_takes_no_stock()
    {
        var (service, products, orders) = Build(stock: 10, quantity: 3);

  
        var result = await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 1m);

        result.Outcome.ShouldBe(PaymentOutcome.AmountMismatch);
        orders.Items[0].Status.ShouldBe(OrderStatus.AwaitingPayment);
        products.Items[0].StockQuantity.ShouldBe(10);
    }

    [Fact]
    public async Task Stock_running_out_between_checkout_and_payment_still_records_the_payment()
    {
        var (service, _, orders) = Build(stock: 1, quantity: 3);

        var result = await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 300m);

        result.Outcome.ShouldBe(PaymentOutcome.Confirmed);
        orders.Items[0].Status.ShouldBe(OrderStatus.Paid);
        result.HasStockShortfall.ShouldBeTrue();
        result.StockShortfalls!.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task A_quotation_cannot_be_paid()
    {
        var (service, products, _) = Build(stock: 10, quantity: 3, quotation: true);

        var result = await service.ConfirmPaymentAsync(OrderId, "PAYFAST", "pf-001", 300m);

        result.Outcome.ShouldBe(PaymentOutcome.NotAwaitingPayment);
        products.Items[0].StockQuantity.ShouldBe(10);
    }

    [Fact]
    public async Task An_unknown_order_is_reported_rather_than_throwing()
    {
        var (service, _, _) = Build(stock: 10, quantity: 3);

        var result = await service.ConfirmPaymentAsync("no-such-order", "PAYFAST", "pf-001", 300m);

        result.Outcome.ShouldBe(PaymentOutcome.OrderNotFound);
    }

    private const string OrderId = "order-1";

    private static (OrderService, MemoryProducts, MemoryOrders) Build(
        int stock, int quantity, bool quotation = false)
    {
        var products = new MemoryProducts();
        products.Items.Add(new Product
        {
            Id = Product,
            Name = "Everyday Non-Stick Frying Pan 20cm",
            Sku = "SS-EV-FP20",
            Price = 100m,
            StockQuantity = stock,
            IsPublished = true
        });

        var order = new Order
        {
            Id = OrderId,
            OrderReference = "SS-20260925-TEST01",
            ClientId = Client,
            OrderType = quotation ? OrderType.QuoteRequest : OrderType.Checkout,
            Status = quotation ? OrderStatus.Pending : OrderStatus.AwaitingPayment,
            Items = [new OrderItem { ProductId = Product, ProductName = "Pan", UnitPrice = 100m, Quantity = quantity }],
            Subtotal = 100m * quantity,
            ShippingCost = 0m,
            Total = 100m * quantity
        };

        var orders = new MemoryOrders();
        orders.Items.Add(order);

        return (new OrderService(new MemoryCarts(), orders, products, new FixedSettings(), new OrderStrategyFactory()),
                products, orders);
    }

    private sealed class MemoryProducts : IProductRepository
    {
        public List<Product> Items { get; } = [];

        public Task<Product?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(p => p.Id == id));

        public Task<Product?> GetBySkuAsync(string sku, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(p => p.Sku == sku));

        public Task<IReadOnlyList<Product>> GetManyAsync(IEnumerable<string> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(Items.Where(p => ids.Contains(p.Id)).ToList());

        public Task<Page<Product>> SearchAsync(ProductFilter filter, CancellationToken ct = default) =>
            Task.FromResult(new Page<Product>(Items, Items.Count, 1, 20));

        public Task<CatalogueFacets> GetFacetsAsync(bool publishedOnly = true, CancellationToken ct = default) =>
            Task.FromResult(CatalogueFacets.Empty);

        public Task<string> CreateAsync(Product product, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> TryDecrementStockAsync(string productId, int quantity, CancellationToken ct = default)
        {
            var p = Items.Find(x => x.Id == productId);

            if (p is null || quantity <= 0 || p.StockQuantity < quantity)
            {
                return Task.FromResult(false);
            }

            p.StockQuantity -= quantity;
            return Task.FromResult(true);
        }

        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MemoryOrders : IOrderRepository
    {
        public List<Order> Items { get; } = [];

        public Task<Order?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(o => o.Id == id));

        public Task<Order?> GetForClientAsync(string id, string clientId, CancellationToken ct = default) =>
            Task.FromResult(Items.Find(o => o.Id == id && o.ClientId == clientId));

        public Task<Page<Order>> ListForClientAsync(string clientId, int pageNumber, int pageSize, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Page<Order>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> CreateAsync(Order order, CancellationToken ct = default)
        {
            Items.Add(order);
            return Task.FromResult(order.Id);
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(string id, CancellationToken ct = default) => Task.CompletedTask;

        public Task<bool> HasDeliveredOrderWithProductAsync(string clientId, string productId, CancellationToken ct = default) =>
            Task.FromResult(false);
    }

    private sealed class MemoryCarts : ICartRepository
    {
        public Task<Cart?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
            Task.FromResult<Cart?>(null);

        public Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct = default) =>
            Task.FromResult<Cart?>(null);

        public Task SaveAsync(Cart cart, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(string cartId, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FixedSettings : ISiteSettingsProvider
    {
        public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) =>
            Task.FromResult(new SiteSettings { BulkOrderThreshold = 5000m, DisplayCurrency = "ZAR" });

        public void Invalidate()
        {
        }
    }
}
