using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Wishlist;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.UnitTests.Admin;

namespace SimmerAndSear.UnitTests.Lookbooks;

public sealed class LookbookServiceTests
{
    private readonly MemoryProductRepository _products = new();
    private readonly MemoryWishlists _wishlists = new();
    private readonly RecordingRenderer _renderer = new();
    private readonly StubMedia _media = new();

    private LookbookService Service() =>
        new(_wishlists, _products, _media, _renderer, new FixedSettings());

    private Product SeedProduct(string id, string name, decimal price, int stock, string? imageId)
    {
        var product = new Product
        {
            Id = id,
            Sku = $"SKU-{id}",
            Name = name,
            Price = price,
            StockQuantity = stock,
            Brand = "Heritage",
            Material = "Enamelled cast iron",
            SizeLabel = "28cm",
            IsPublished = true
        };

        if (imageId is not null)
        {
            product.Images.Add(new ProductImage
            {
                ProductImageId = "img-1",
                MediaAssetId = imageId,
                AltText = name,
                IsPrimary = true
            });
        }

        return _products.Add(product);
    }

    private void SeedWishlist(string clientId, params Product[] products)
    {
        var wishlist = new SimmerAndSear.Domain.Entities.Wishlist { ClientId = clientId };

        foreach (var product in products)
        {
            wishlist.Add(product.Id, product.Name, product.Price);
        }

        _wishlists.Save(wishlist);
    }

    [Fact]
    public async Task An_empty_wishlist_has_no_lookbook()
    {
        (await Service().BuildAsync("client-1", "Thandi")).ShouldBeNull();
        _renderer.Rendered.ShouldBeNull();
    }

    [Fact]
    public async Task Builds_a_named_pdf_from_the_saved_products()
    {
        var pan = SeedProduct("p1", "Heritage Skillet 26cm", 1299m, 4, "media-1");
        SeedWishlist("client-1", pan);

        var file = await Service().BuildAsync("client-1", "Thandi Mokoena");

        file.ShouldNotBeNull();
        file.FileName.ShouldStartWith("Lookbook-");
        file.FileName.ShouldEndWith(".pdf");

        var lookbook = _renderer.Rendered.ShouldNotBeNull();
        lookbook.CustomerName.ShouldBe("Thandi Mokoena");
        lookbook.Currency.ShouldBe("ZAR");

        var item = lookbook.Items.ShouldHaveSingleItem();
        item.ProductName.ShouldBe("Heritage Skillet 26cm");
        item.Brand.ShouldBe("Heritage");
        item.SizeLabel.ShouldBe("28cm");
        item.InStock.ShouldBeTrue();
        item.Image.ShouldNotBeNull();
    }

    [Fact]
    public async Task Uses_the_live_price_not_the_price_when_it_was_saved()
    {
        var pot = SeedProduct("p1", "Heritage Casserole 28cm", 2799m, 2, null);
        SeedWishlist("client-1", pot);

        pot.Price = 3199m;

        await Service().BuildAsync("client-1", "Thandi");

        _renderer.Rendered!.Items[0].UnitPrice.ShouldBe(3199m);
        _renderer.Rendered!.Total.ShouldBe(3199m);
    }

    [Fact]
    public async Task Marks_a_sold_out_product_as_out_of_stock()
    {
        SeedWishlist("client-1", SeedProduct("p1", "Signature Wok 30cm", 1499m, 0, null));

        await Service().BuildAsync("client-1", "Thandi");

        _renderer.Rendered!.Items[0].InStock.ShouldBeFalse();
    }

    [Fact]
    public async Task A_withdrawn_product_keeps_its_saved_name_and_price()
    {
        SeedWishlist("client-1", new Product { Id = "gone", Name = "Discontinued Pan", Price = 999m });

        await Service().BuildAsync("client-1", "Thandi");

        var item = _renderer.Rendered!.Items.ShouldHaveSingleItem();
        item.ProductName.ShouldBe("Discontinued Pan");
        item.UnitPrice.ShouldBe(999m);
        item.InStock.ShouldBeFalse();
        item.Image.ShouldBeNull();
    }

    [Fact]
    public async Task An_unreadable_image_costs_the_card_its_photo_not_the_document()
    {
        SeedWishlist("client-1", SeedProduct("p1", "Heritage Skillet 26cm", 1299m, 4, "media-1"));
        _media.Throw = true;

        var file = await Service().BuildAsync("client-1", "Thandi");

        file.ShouldNotBeNull();
        _renderer.Rendered!.Items[0].Image.ShouldBeNull();
    }

    private sealed class RecordingRenderer : IWishlistLookbookRenderer
    {
        public Lookbook? Rendered { get; private set; }

        public byte[] Render(Lookbook lookbook)
        {
            Rendered = lookbook;
            return [1, 2, 3];
        }
    }

    private sealed class StubMedia : IMediaService
    {
        public bool Throw { get; set; }

        public Task<MediaAsset> UploadAsync(
            Stream stream, string rawFileName, long fileSizeBytes, string? uploadedBy,
            CancellationToken ct = default) =>
            Task.FromResult(new MediaAsset());

        public Task<MediaStreamResult?> GetOriginalStreamAsync(string id, CancellationToken ct = default) =>
            Task.FromResult<MediaStreamResult?>(null);

        public Task<MediaStreamResult?> GetThumbnailStreamAsync(string id, CancellationToken ct = default)
        {
            if (Throw)
            {
                throw new InvalidOperationException("GridFS is unavailable.");
            }

            return Task.FromResult<MediaStreamResult?>(
                new MediaStreamResult(new MemoryStream([9, 9, 9]), "image/webp", "\"etag\"", 3));
        }
    }

    private sealed class MemoryWishlists : IWishlistRepository
    {
        private readonly Dictionary<string, SimmerAndSear.Domain.Entities.Wishlist> _byClient = [];

        public void Save(SimmerAndSear.Domain.Entities.Wishlist wishlist) => _byClient[wishlist.ClientId] = wishlist;

        public Task<SimmerAndSear.Domain.Entities.Wishlist?> GetByClientAsync(string clientId, CancellationToken ct = default) =>
            Task.FromResult(_byClient.GetValueOrDefault(clientId));

        public Task SaveAsync(SimmerAndSear.Domain.Entities.Wishlist wishlist, CancellationToken ct = default)
        {
            Save(wishlist);
            return Task.CompletedTask;
        }

        public Task DeleteAsync(string clientId, CancellationToken ct = default)
        {
            _byClient.Remove(clientId);
            return Task.CompletedTask;
        }
    }

    private sealed class FixedSettings : ISiteSettingsProvider
    {
        public Task<SiteSettings> CurrentAsync(CancellationToken ct = default) =>
            Task.FromResult(new SiteSettings { BulkOrderThreshold = 5000m, DisplayCurrency = "ZAR" });

        public void Invalidate() { }
    }
}
