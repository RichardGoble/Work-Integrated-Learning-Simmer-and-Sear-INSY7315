using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Application.Quotations;

//References
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 21 September 2026].

//Microsoft, 2026. MemoryStream Class [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/system.io.memorystream> [Accessed 21 September 2026].

//Microsoft, 2026. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 21 September 2026].

public sealed record QuotationFile(Stream Content, string FileName);

public sealed class QuotationService(
    IOrderRepository orders,
    IQuotationDocumentRepository documents,
    IFileStore files,
    IQuotationPdfRenderer renderer,
    ISiteSettingsProvider settings)
{
    private const string PdfContentType = "application/pdf";

    public async Task<QuotationFile?> GetForClientAsync(string orderId, string clientId, CancellationToken ct = default) =>
        await GetOrCreateAsync(await orders.GetForClientAsync(orderId, clientId, ct), ct);

    public async Task<QuotationFile?> GetForStaffAsync(string orderId, CancellationToken ct = default) =>
        await GetOrCreateAsync(await orders.GetAsync(orderId, ct), ct);

    private async Task<QuotationFile?> GetOrCreateAsync(Order? order, CancellationToken ct)
    {
        if (order is null || order.OrderType != OrderType.QuoteRequest)
        {
            return null;
        }

        var existing = await documents.GetByOrderAsync(order.Id, ct);
        if (existing is not null)
        {
            return await OpenAsync(existing, ct);
        }

        var site = await settings.CurrentAsync(ct);
        var validUntil = order.PlacedAt.AddDays(site.QuotationValidityDays);
        var fileName = $"Quotation-{order.OrderReference}.pdf";
        var bytes = renderer.Render(order, order.ShippingDetails?.Email ?? string.Empty, validUntil);

        string gridFsId;
        using (var upload = new MemoryStream(bytes, writable: false))
        {
            gridFsId = await files.UploadAsync(upload, fileName, PdfContentType, ct);
        }

        var document = new QuotationDocument
        {
            OrderId = order.Id,
            OrderReference = order.OrderReference,
            ClientId = order.ClientId,
            GridFsId = gridFsId,
            FileName = fileName,
            FileSizeBytes = bytes.LongLength,
            Total = order.Total,
            Currency = order.Currency,
            ValidUntil = validUntil,
            GeneratedAt = DateTime.UtcNow
        };

        if (!await documents.TryCreateAsync(document, ct))
        {
            await files.DeleteAsync(gridFsId, ct);

            var winner = await documents.GetByOrderAsync(order.Id, ct);
            return winner is null ? null : await OpenAsync(winner, ct);
        }

        return new QuotationFile(new MemoryStream(bytes, writable: false), fileName);
    }

    private async Task<QuotationFile> OpenAsync(QuotationDocument document, CancellationToken ct) =>
        new(await files.OpenDownloadAsync(document.GridFsId, ct), document.FileName);
}
