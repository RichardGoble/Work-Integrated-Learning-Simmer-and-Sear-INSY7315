using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Application.Wishlist;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Contracts.Wishlist;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.UnitTests.Admin;

namespace SimmerAndSear.UnitTests.Accounts;

//References
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 21 September 2026].

//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 21 September 2026].


public sealed class WishlistServiceTests
{
    private const string ClientId = "client-01";
    private const string ProductId = "650000000000000000000001";

    private readonly MemoryWishlistRepository _wishlists = new();
    private readonly MemoryProductRepository _products = new();
    private readonly MemoryCartRepository _carts = new();
    private readonly MemoryOrderRepository _orders = new();

    private static readonly WishlistQuoteRequest Quote = new()
    {
        Name = "Quote Customer", Email = "quote@example.com", Company = "  Acme Kitchens ", Notes = "   "
    };

    private WishlistService CreateService() =>
        new(_wishlists, _products, new CartService(_carts, _products, new FixedSiteSettings()), _orders);

    [Fact]
    public async Task Adding_same_product_twice_keeps_one_line()
    {
        _products.Add(new Product { Id = ProductId, Name = "Cast Iron Pan", Price = 499m, IsPublished = true });
        var service = CreateService();

        await service.AddAsync(ClientId, ProductId);
        var (wishlist, error) = await service.AddAsync(ClientId, ProductId);

        error.ShouldBeNull();
        wishlist.ShouldNotBeNull();
        wishlist!.Count.ShouldBe(1);
        wishlist.Items[0].ProductName.ShouldBe("Cast Iron Pan");
        wishlist.Items[0].UnitPrice.ShouldBe(499m);
    }

    [Fact]
    public async Task Unpublished_product_cannot_be_added()
    {
        _products.Add(new Product { Id = ProductId, Name = "Draft Pan", Price = 299m, IsPublished = false });
        var service = CreateService();

        var (wishlist, error) = await service.AddAsync(ClientId, ProductId);

        wishlist.ShouldBeNull();
        error.ShouldNotBeNullOrEmpty();
        (await service.GetAsync(ClientId)).Count.ShouldBe(0);
    }

    [Fact]
    public async Task Move_to_cart_adds_one_unit_and_removes_the_wishlist_line()
    {
        _products.Add(new Product { Id = ProductId, Name = "Cast Iron Pan", Price = 499m, IsPublished = true, StockQuantity = 5 });
        var service = CreateService();
        await service.AddAsync(ClientId, ProductId);

        var result = await service.MoveToCartAsync(ClientId, ProductId);

        result.Outcome.ShouldBe(WishlistMoveOutcome.Moved);
        result.Result.ShouldNotBeNull();
        result.Result!.Cart.Items.ShouldHaveSingleItem().Quantity.ShouldBe(1);
        result.Result.Wishlist.Count.ShouldBe(0);
        (await service.GetAsync(ClientId)).Items.ShouldNotContain(i => i.ProductId == ProductId);
    }

    [Fact]
    public async Task Out_of_stock_item_stays_in_wishlist_when_move_fails()
    {
        _products.Add(new Product { Id = ProductId, Name = "Cast Iron Pan", Price = 499m, IsPublished = true, StockQuantity = 0 });
        var service = CreateService();
        await service.AddAsync(ClientId, ProductId);

        var result = await service.MoveToCartAsync(ClientId, ProductId);

        result.Outcome.ShouldBe(WishlistMoveOutcome.OutOfStock);
        (await service.GetAsync(ClientId)).Items.ShouldContain(i => i.ProductId == ProductId);
        (await _carts.GetByClientAsync(ClientId)).ShouldBeNull();
    }

    [Fact]
    public async Task Quote_request_places_a_quote_order_at_live_prices_and_keeps_the_wishlist()
    {
        var product = new Product { Id = ProductId, Name = "Cast Iron Pan", Price = 499m, IsPublished = true, StockQuantity = 5 };
        _products.Add(product);
        var service = CreateService();
        await service.AddAsync(ClientId, ProductId);
        product.Price = 549m;

        var result = await service.RequestQuoteAsync(ClientId, Quote);

        result.Outcome.ShouldBe(WishlistQuoteOutcome.Placed);
        result.Order!.IsQuotation.ShouldBeTrue();
        result.Order.Total.ShouldBe(549m);
        var order = _orders.Items.ShouldHaveSingleItem();
        order.OrderType.ShouldBe(Domain.Enums.OrderType.QuoteRequest);
        order.Items.ShouldHaveSingleItem().Quantity.ShouldBe(1);
        order.CompanyName.ShouldBe("Acme Kitchens");
        order.CustomerNotes.ShouldBeNull();
        (await service.GetAsync(ClientId)).Count.ShouldBe(1);
    }

    [Fact]
    public async Task Quote_request_is_refused_for_an_empty_wishlist_or_an_out_of_stock_line()
    {
        var service = CreateService();
        (await service.RequestQuoteAsync(ClientId, Quote)).Outcome.ShouldBe(WishlistQuoteOutcome.EmptyWishlist);

        _products.Add(new Product { Id = ProductId, Name = "Cast Iron Pan", Price = 499m, IsPublished = true, StockQuantity = 0 });
        await service.AddAsync(ClientId, ProductId);

        var result = await service.RequestQuoteAsync(ClientId, Quote);

        result.Outcome.ShouldBe(WishlistQuoteOutcome.LineUnavailable);
        result.Detail.ShouldNotBeNullOrEmpty();
        _orders.Items.ShouldBeEmpty();
        (await service.GetAsync(ClientId)).Count.ShouldBe(1);
    }

    private sealed class MemoryOrderRepository : IOrderRepository
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
            order.Id = Guid.NewGuid().ToString("N");
            Items.Add(order);
            return Task.FromResult(order.Id);
        }

        public Task UpdateAsync(Order order, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class MemoryCartRepository : ICartRepository
    {
        private readonly List<Cart> _items = [];

        public Task<Cart?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
            Task.FromResult(_items.Find(c => c.ClientId == clientId));

        public Task<Cart?> GetBySessionAsync(string sessionId, CancellationToken ct = default) =>
            Task.FromResult(_items.Find(c => c.SessionId == sessionId));

        public Task SaveAsync(Cart cart, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(cart.Id))
            {
                cart.Id = Guid.NewGuid().ToString("N");
                _items.Add(cart);
            }

            return Task.CompletedTask;
        }

        public Task DeleteAsync(string cartId, CancellationToken ct = default)
        {
            _items.RemoveAll(c => c.Id == cartId);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedSiteSettings : ISiteSettingsProvider
    {
        public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) =>
            Task.FromResult(new SiteSettings());

        public void Invalidate()
        {
        }
    }

    private sealed class MemoryWishlistRepository : IWishlistRepository
    {
        private readonly List<Wishlist> _items = [];

        public Task<Wishlist?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
            Task.FromResult(_items.Find(w => w.ClientId == clientId));

        public Task SaveAsync(Wishlist wishlist, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(wishlist.Id))
            {
                wishlist.Id = Guid.NewGuid().ToString("N");
                _items.Add(wishlist);
            }

            return Task.CompletedTask;
        }
    }
}
