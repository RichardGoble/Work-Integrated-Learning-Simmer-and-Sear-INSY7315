using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Wishlist;

namespace SimmerAndSear.Application.Wishlist;

//References
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2025. Tuple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/value-tuples> [Accessed 21 September 2026].

//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 21 September 2026].


public sealed class WishlistService(IWishlistRepository wishlists, IProductRepository products)
{
    private static readonly WishlistResponse Empty = new([], 0);

    public async Task<WishlistResponse> GetAsync(string clientId, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);
        return wishlist is null ? Empty : Describe(wishlist);
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
        return (Describe(wishlist), null);
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

        return Describe(wishlist);
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

    private static WishlistResponse Describe(Domain.Entities.Wishlist wishlist)
    {
        var items = wishlist.Items
            .Select(i => new WishlistItemResponse(i.ProductId, i.ProductName, i.UnitPrice, i.AddedAt))
            .ToList();

        return new WishlistResponse(items, items.Count);
    }
}
