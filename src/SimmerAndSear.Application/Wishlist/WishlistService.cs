using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Contracts.Wishlist;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Wishlist;

//References
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2025. Tuple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples> [Accessed 21 September 2026].

//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 21 September 2026].

//Refactoring.Guru, n.d. Facade [Source code].
//Available at: <https://refactoring.guru/design-patterns/facade> [Accessed 21 September 2026].


public sealed class WishlistService(
    IWishlistRepository wishlists, IProductRepository products, CartService cart, IOrderRepository orders)
{
    private static readonly WishlistResponse Empty = new([], 0);

    public async Task<WishlistResponse> GetAsync(string clientId, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);
        return wishlist is null ? Empty : await DescribeAsync(wishlist, ct);
    }

    public async Task<(WishlistResponse? Wishlist, string? Error)> AddAsync(
        string clientId, string productId, CancellationToken ct = default)
    {
        var product = await products.GetAsync(productId, ct);
        if (product is null || !product.IsPublished)
        {
            return (null, "Product not found.");
        }

        var wishlist = await wishlists.GetByClientAsync(clientId, ct)
            ?? new Domain.Entities.Wishlist { ClientId = clientId };

        if (!wishlist.Add(product.Id, product.Name, product.Price))
        {
            return (null, $"A wishlist can hold at most {Domain.Entities.Wishlist.MaxItems} items.");
        }

        await wishlists.SaveAsync(wishlist, ct);
        return (await DescribeAsync(wishlist, ct), null);
    }

    public async Task<WishlistResponse> RemoveAsync(string clientId, string productId, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);
        if (wishlist is null)
        {
            return Empty;
        }

        if (wishlist.Remove(productId))
        {
            await wishlists.SaveAsync(wishlist, ct);
        }

        return await DescribeAsync(wishlist, ct);
    }

    public async Task ClearAsync(string clientId, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);
        if (wishlist is null || wishlist.Items.Count == 0)
        {
            return;
        }

        wishlist.Clear();
        await wishlists.SaveAsync(wishlist, ct);
    }

    public async Task<WishlistMoveResult> MoveToCartAsync(
        string clientId, string productId, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);
        if (wishlist is null || !wishlist.Items.Any(i => i.ProductId == productId))
        {
            return new WishlistMoveResult(WishlistMoveOutcome.NotInWishlist, null);
        }

        var result = await cart.AddAsync(CartOwner.ForClient(clientId), productId, 1, ct);

        switch (result.Outcome)
        {
            case CartChange.ProductNotFound:
                return new WishlistMoveResult(WishlistMoveOutcome.Unavailable, null);
            case CartChange.OutOfStock:
                return new WishlistMoveResult(WishlistMoveOutcome.OutOfStock, null);
        }

        wishlist.Remove(productId);
        await wishlists.SaveAsync(wishlist, ct);

        var response = new MoveToCartResponse(await DescribeAsync(wishlist, ct), result.Cart!);
        return new WishlistMoveResult(WishlistMoveOutcome.Moved, response);
    }

    public async Task<WishlistQuoteResult> RequestQuoteAsync(
        string clientId, WishlistQuoteRequest request, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);
        if (wishlist is null || wishlist.Items.Count == 0)
        {
            return new WishlistQuoteResult(WishlistQuoteOutcome.EmptyWishlist, null);
        }

        var catalogue = (await products.GetManyAsync(wishlist.Items.Select(i => i.ProductId), ct))
            .ToDictionary(p => p.Id);

        var lines = new List<OrderItem>();
        foreach (var item in wishlist.Items)
        {
            if (!catalogue.TryGetValue(item.ProductId, out var product) || !product.IsPublished)
            {
                return new WishlistQuoteResult(
                    WishlistQuoteOutcome.LineUnavailable, null, $"{item.ProductName} is no longer available.");
            }

            if (product.StockQuantity < 1)
            {
                return new WishlistQuoteResult(
                    WishlistQuoteOutcome.LineUnavailable, null, $"{product.Name} is out of stock. Please remove it from your wishlist.");
            }

            lines.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Quantity = 1
            });
        }

        var contact = new ShippingDetails { FullName = request.Name.Trim(), Email = request.Email.Trim() };
        var order = Order.FromWishlistQuote(lines, clientId, contact, Blank(request.Company), Blank(request.Notes));

        await orders.CreateAsync(order, ct);

        var described = OrderService.Describe(order, new QuotationFulfilmentStrategy().NextStepMessage);
        return new WishlistQuoteResult(WishlistQuoteOutcome.Placed, described);
    }

    private static string? Blank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private async Task<WishlistResponse> DescribeAsync(Domain.Entities.Wishlist wishlist, CancellationToken ct)
    {
        var catalogue = wishlist.Items.Count == 0
            ? new Dictionary<string, Product>()
            : (await products.GetManyAsync(wishlist.Items.Select(i => i.ProductId), ct))
                .ToDictionary(p => p.Id);

        var items = wishlist.Items.Select(item =>
        {
            catalogue.TryGetValue(item.ProductId, out var product);

            return new WishlistItemResponse(
                item.ProductId,
                product?.Name ?? item.ProductName,
                product?.Price ?? item.UnitPrice,
                item.AddedAt,
                product?.PrimaryImage?.MediaAssetId,
                product?.PrimaryImage?.AltText,
                product is { IsPublished: true } && product.StockQuantity > 0,
                product?.Brand,
                product?.Material);
        }).ToList();

        return new WishlistResponse(items, items.Count);
    }
}
