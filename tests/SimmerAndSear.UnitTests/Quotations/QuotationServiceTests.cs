using System.Text;
using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Quotations;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Infrastructure.Documents;

namespace SimmerAndSear.UnitTests.Quotations;

//References
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 21 September 2026].

//Shouldly, n.d. Shouldly Documentation [Source code].
//Available at: <https://docs.shouldly.org/> [Accessed 21 September 2026].

//QuestPDF, n.d. Getting started [Source code].
//Available at: <https://www.questpdf.com/quick-start.html> [Accessed 21 September 2026].

public sealed class QuotationServiceTests
{
    private const string OwnerId = "client-01";
    private const string OtherId = "client-02";
    private const string QuoteOrderId = "650000000000000000000002";
    private const string CheckoutOrderId = "650000000000000000000003";

    private static readonly DateTime PlacedAt = new(2026, 9, 1, 10, 0, 0, DateTimeKind.Utc);

    private readonly MemoryOrderRepository _orders = new();
    private readonly MemoryQuotationDocumentRepository _documents = new();
    private readonly MemoryFileStore _files = new();

    public QuotationServiceTests()
    {
        _orders.Items.Add(CreateOrder(QuoteOrderId, OrderType.QuoteRequest));
        _orders.Items.Add(CreateOrder(CheckoutOrderId, OrderType.Checkout));
    }

    private QuotationService CreateService() =>
        new(_orders, _documents, _files, new QuestQuotationPdfRenderer(), new FixedSiteSettings());

    [Fact]
    public async Task Quotation_is_rendered_once_then_served_from_the_store()
    {
        var service = CreateService();

        var first = await service.GetForClientAsync(QuoteOrderId, OwnerId);
        var second = await service.GetForClientAsync(QuoteOrderId, OwnerId);

        first.ShouldNotBeNull();
        second.ShouldNotBeNull();
        first.FileName.ShouldBe("Quotation-SS-TEST-0002.pdf");
        StartsWithPdfHeader(first.Content).ShouldBeTrue();
        StartsWithPdfHeader(second.Content).ShouldBeTrue();

        _files.Files.Count.ShouldBe(1);
        _documents.Items.Count.ShouldBe(1);
        _documents.Items[0].ValidUntil.ShouldBe(PlacedAt.AddDays(30));
    }

    [Fact]
    public async Task Checkout_orders_and_other_customers_get_no_quotation()
    {
        var service = CreateService();

        (await service.GetForClientAsync(CheckoutOrderId, OwnerId)).ShouldBeNull();
        (await service.GetForClientAsync(QuoteOrderId, OtherId)).ShouldBeNull();

        _files.Files.ShouldBeEmpty();
    }

    private static Order CreateOrder(string id, OrderType type) => new()
    {
        Id = id,
        OrderReference = $"SS-TEST-{id[^4..]}",
        ClientId = OwnerId,
        OrderType = type,
        Status = OrderStatus.Pending,
        Items = [new OrderItem { ProductId = "650000000000000000000001", ProductName = "Cast Iron Pan", UnitPrice = 499m, Quantity = 20 }],
        Subtotal = 9980m,
        Total = 9980m,
        Currency = "ZAR",
        ShippingDetails = new ShippingDetails
        {
            FullName = "Test Customer",
            Email = "customer@example.com",
            AddressLine = "1 Test Street",
            City = "Cape Town",
            PostalCode = "8001"
        },
        PlacedAt = PlacedAt,
        UpdatedAt = PlacedAt
    };

    private static bool StartsWithPdfHeader(Stream content)
    {
        using var reader = new StreamReader(content, Encoding.ASCII);
        var header = new char[4];
        return reader.Read(header, 0, 4) == 4 && new string(header) == "%PDF";
    }

    private sealed class MemoryOrderRepository : IOrderRepository
    {
        public List<Order> Items { get; } = [];

        public Task<Order?> GetAsync(string id, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(o => o.Id == id));

        public Task<Order?> GetForClientAsync(string id, string clientId, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(o => o.Id == id && o.ClientId == clientId));

        public Task<Page<Order>> ListForClientAsync(
            string clientId, int pageNumber, int pageSize, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<Page<Order>> ListAsync(int pageNumber, int pageSize, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task<string> CreateAsync(Order order, CancellationToken ct = default) =>
            throw new NotSupportedException();

        public Task UpdateAsync(Order order, CancellationToken ct = default) =>
            throw new NotSupportedException();
    }

    private sealed class MemoryQuotationDocumentRepository : IQuotationDocumentRepository
    {
        public List<QuotationDocument> Items { get; } = [];

        public Task<QuotationDocument?> GetByOrderAsync(string orderId, CancellationToken ct = default) =>
            Task.FromResult(Items.FirstOrDefault(d => d.OrderId == orderId));

        public Task<bool> TryCreateAsync(QuotationDocument document, CancellationToken ct = default)
        {
            if (Items.Any(d => d.OrderId == document.OrderId))
            {
                return Task.FromResult(false);
            }

            Items.Add(document);
            return Task.FromResult(true);
        }
    }

    private sealed class MemoryFileStore : IFileStore
    {
        public Dictionary<string, byte[]> Files { get; } = [];

        public async Task<string> UploadAsync(
            Stream content, string fileName, string contentType, CancellationToken ct = default)
        {
            using var buffer = new MemoryStream();
            await content.CopyToAsync(buffer, ct);

            var id = Guid.NewGuid().ToString("N");
            Files[id] = buffer.ToArray();
            return id;
        }

        public Task<Stream> OpenDownloadAsync(string fileId, CancellationToken ct = default) =>
            Task.FromResult<Stream>(new MemoryStream(Files[fileId], writable: false));

        public Task DeleteAsync(string fileId, CancellationToken ct = default)
        {
            Files.Remove(fileId);
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
}
