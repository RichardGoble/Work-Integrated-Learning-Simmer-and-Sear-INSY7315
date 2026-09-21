using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Wishlist;
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

    private WishlistService CreateService() => new(_wishlists, _products);

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
