using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Application.Wishlist;

//References
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 30 September 2026].

//Microsoft, 2026. MemoryStream Class [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.io.memorystream> [Accessed 30 September 2026].

//Microsoft, 2026. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 30 September 2026].

public sealed record LookbookFile(byte[] Content, string FileName);


public sealed class LookbookService(
    IWishlistRepository wishlists,
    IProductRepository products,
    IMediaService media,
    IWishlistLookbookRenderer renderer,
    ISiteSettingsProvider settings)
{
    public async Task<LookbookFile?> BuildAsync(
        string clientId, string customerName, CancellationToken ct = default)
    {
        var wishlist = await wishlists.GetByClientAsync(clientId, ct);

        if (wishlist is null || wishlist.Items.Count == 0)
        {
            return null;
        }

        var site = await settings.CurrentAsync(ct);

        var catalogue = (await products.GetManyAsync(wishlist.Items.Select(i => i.ProductId), ct))
            .ToDictionary(p => p.Id);

        var items = new List<LookbookItem>(wishlist.Items.Count);

        foreach (var saved in wishlist.Items)
        {
            catalogue.TryGetValue(saved.ProductId, out var product);

            items.Add(new LookbookItem(
                ProductName: product?.Name ?? saved.ProductName,
                Brand: product?.Brand,
                Material: product?.Material,
                SizeLabel: product?.SizeLabel,
                UnitPrice: product?.Price ?? saved.UnitPrice,
                InStock: product is { StockQuantity: > 0 },
                Image: await ThumbnailAsync(product?.PrimaryImage?.MediaAssetId, ct)));
        }

        var generatedAt = DateTime.UtcNow;

        var lookbook = new Lookbook(
            customerName,
            site.DisplayCurrency,
            generatedAt,
            items);

        return new LookbookFile(
            renderer.Render(lookbook),
            $"Lookbook-{generatedAt:yyyyMMdd}.pdf");
    }

    private async Task<byte[]?> ThumbnailAsync(string? mediaAssetId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(mediaAssetId))
        {
            return null;
        }

  
        try
        {
            var thumbnail = await media.GetThumbnailStreamAsync(mediaAssetId, ct);

            if (thumbnail is null)
            {
                return null;
            }

            await using var source = thumbnail.ContentStream;
            using var buffer = new MemoryStream();
            await source.CopyToAsync(buffer, ct);

            return buffer.ToArray();
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }
}
