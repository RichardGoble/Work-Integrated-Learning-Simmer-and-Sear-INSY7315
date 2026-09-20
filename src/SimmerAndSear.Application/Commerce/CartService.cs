using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//OWASP Foundation, n.d. Business Logic Security - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Business_Logic_Security_Cheat_Sheet.html> [Accessed 18 September 2026].
//
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class CartService(
    ICartRepository carts,
    IProductRepository products,
    ISiteSettingsProvider settings)
{
  
    public static readonly TimeSpan AnonymousCartLifetime = TimeSpan.FromDays(30);

    private const int MaximumLineQuantity = 999;

  
    public async Task<CartResponse> ViewAsync(CartOwner? owner, CancellationToken ct = default)
    {
        var cart = owner is null ? new Cart() : await LoadAsync(owner, ct);
        return await DescribeAsync(cart, ct);
    }

    
    
    public async Task<CartChangeResult> AddAsync(
        CartOwner owner, string productId, int quantity, CancellationToken ct = default)
    {
        if (quantity <= 0)
        {
            return CartChangeResult.OutOfStock();
        }

        var product = await products.GetAsync(productId, ct);

        
        if (product is null || !product.IsPublished)
        {
            return CartChangeResult.ProductNotFound();
        }

        var cart = await LoadAsync(owner, ct);
        var onHand = cart.Items.FirstOrDefault(i => i.ProductId == product.Id)?.Quantity ?? 0;
        var target = Clamp(onHand + quantity, product);

        if (target <= 0)
        {
            return CartChangeResult.OutOfStock();
        }

        cart.SetLine(product.Id, product.Name, product.Price, target);
        await PersistAsync(cart, owner, ct);

        return CartChangeResult.Updated(await DescribeAsync(cart, ct));
    }

    
    
    public async Task<CartChangeResult> SetQuantityAsync(
        CartOwner owner, string productId, int quantity, CancellationToken ct = default)
    {
        var cart = await LoadAsync(owner, ct);

       
        if (quantity <= 0)
        {
            cart.RemoveItem(productId);
            await PersistAsync(cart, owner, ct);
            return CartChangeResult.Updated(await DescribeAsync(cart, ct));
        }

        var product = await products.GetAsync(productId, ct);

        if (product is null || !product.IsPublished)
        {
            return CartChangeResult.ProductNotFound();
        }

        var target = Clamp(quantity, product);

        if (target <= 0)
        {
            return CartChangeResult.OutOfStock();
        }

        cart.SetLine(product.Id, product.Name, product.Price, target);
        await PersistAsync(cart, owner, ct);

        return CartChangeResult.Updated(await DescribeAsync(cart, ct));
    }

    public async Task<CartResponse> RemoveAsync(
        CartOwner owner, string productId, CancellationToken ct = default)
    {
        var cart = await LoadAsync(owner, ct);
        cart.RemoveItem(productId);

        await PersistAsync(cart, owner, ct);
        return await DescribeAsync(cart, ct);
    }

    
    public async Task<CartResponse> ClaimAsync(
        string clientId, string sessionId, CancellationToken ct = default)
    {
        var owner = CartOwner.ForClient(clientId);
        var anonymous = await carts.GetBySessionAsync(sessionId, ct);

        if (anonymous is null)
        {
            return await ViewAsync(owner, ct);
        }

        var mine = await LoadAsync(owner, ct);

        if (!anonymous.IsEmpty)
        {
            mine.MergeFrom(anonymous);
            await RepriceAsync(mine, ct);
            await PersistAsync(mine, owner, ct);
        }

       
        await carts.DeleteAsync(anonymous.Id, ct);

        return await DescribeAsync(mine, ct);
    }

    public async Task<CartResponse> ClearAsync(CartOwner owner, CancellationToken ct = default)
    {
        var cart = await LoadAsync(owner, ct);
        cart.Clear();

        await PersistAsync(cart, owner, ct);
        return await DescribeAsync(cart, ct);
    }

    
    public async Task<Cart> LoadAsync(CartOwner owner, CancellationToken ct = default)
    {
        var existing = owner.ClientId is { } clientId
            ? await carts.GetByClientAsync(clientId, ct)
            : await carts.GetBySessionAsync(owner.SessionId!, ct);

        return existing ?? new Cart { ClientId = owner.ClientId, SessionId = owner.SessionId };
    }

    private async Task PersistAsync(Cart cart, CartOwner owner, CancellationToken ct)
    {
        cart.ClientId = owner.ClientId;
        cart.SessionId = owner.SessionId;

       
        cart.ExpiresAt = owner.IsAnonymous ? DateTime.UtcNow.Add(AnonymousCartLifetime) : null;

        await carts.SaveAsync(cart, ct);
    }

    
    private async Task<CartResponse> DescribeAsync(Cart cart, CancellationToken ct)
    {
        var site = await settings.CurrentAsync(ct);

        var catalogue = cart.IsEmpty
            ? new Dictionary<string, Product>()
            : (await products.GetManyAsync(cart.Items.Select(i => i.ProductId), ct))
                .ToDictionary(p => p.Id);

        var lines = cart.Items.Select(item =>
        {
            catalogue.TryGetValue(item.ProductId, out var product);

            return new CartLineResponse(
                item.ProductId,
                product?.Name ?? item.ProductName,
                product?.Brand ?? string.Empty,
                product?.Material,
                product?.PrimaryImage?.AltText,
                item.UnitPrice,
                item.Quantity,
                item.LineTotal,
                product?.StockQuantity ?? 0,

                product is { IsPublished: true } && product.StockQuantity >= item.Quantity);
        }).ToList();

        return new CartResponse(
            lines,
            cart.TotalQuantity,
            cart.Subtotal,
            site.DisplayCurrency,
            site.BulkOrderThreshold,
            cart.ExceedsThreshold(site.BulkOrderThreshold));
    }


    private async Task RepriceAsync(Cart cart, CancellationToken ct)
    {
        if (cart.IsEmpty)
        {
            return;
        }

        var catalogue = (await products.GetManyAsync(cart.Items.Select(i => i.ProductId), ct))
            .ToDictionary(p => p.Id);

        foreach (var line in cart.Items.ToList())
        {
            if (!catalogue.TryGetValue(line.ProductId, out var product) || !product.IsPublished)
            {
                continue;
            }

            var target = Clamp(line.Quantity, product);

            if (target <= 0)
            {
                continue;
            }

            cart.SetLine(product.Id, product.Name, product.Price, target);
        }
    }

    private static int Clamp(int quantity, Product product) =>
        Math.Clamp(quantity, 0, Math.Min(MaximumLineQuantity, product.StockQuantity));
}
