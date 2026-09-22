using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Infrastructure.Documents;

//References
//QuestPDF, n.d. Getting started [Source code].
//Available at: <https://www.questpdf.com/quick-start.html> [Accessed 21 September 2026].

//QuestPDF, n.d. Table [Source code].
//Available at: <https://www.questpdf.com/api-reference/table/basics.html> [Accessed 21 September 2026].

//QuestPDF, n.d. Page number [Source code].
//Available at: <https://www.questpdf.com/api-reference/text/page-numbers.html> [Accessed 21 September 2026].

//QuestPDF, n.d. License configuration [Source code].
//Available at: <https://www.questpdf.com/license/configuration.html> [Accessed 21 September 2026].

//Microsoft, 2026. Standard numeric format strings [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings> [Accessed 21 September 2026].

public sealed class QuestQuotationPdfRenderer : IQuotationPdfRenderer
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;
    private static readonly string Terms = new QuotationFulfilmentStrategy().NextStepMessage;

    static QuestQuotationPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(Order order, string customerEmail, DateTime validUntil)
    {
        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(2, Unit.Centimetre);
                page.DefaultTextStyle(x => x.FontSize(10));

                page.Header().Column(column =>
                {
                    column.Item().Row(row =>
                    {
                        row.RelativeItem().Text("Simmer & Sear").FontSize(20).Bold();
                        row.RelativeItem().AlignRight().Text("QUOTATION").FontSize(20).Bold();
                    });
                    column.Item().PaddingTop(5).Text($"Reference: {order.OrderReference}");
                    column.Item().Text($"Date placed: {order.PlacedAt.ToString("d MMMM yyyy", Invariant)}");
                    column.Item().Text($"Valid until {validUntil.ToString("d MMMM yyyy", Invariant)}");
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(10);

                    column.Item().Column(customer =>
                    {
                        customer.Item().Text("Quotation for").Bold();

                        foreach (var line in CustomerLines(order.ShippingDetails, order.CompanyName, customerEmail))
                        {
                            customer.Item().Text(line);
                        }
                    });

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            columns.RelativeColumn(4);
                            columns.RelativeColumn(1);
                            columns.RelativeColumn(2);
                            columns.RelativeColumn(2);
                        });

                        table.Header(header =>
                        {
                            header.Cell().BorderBottom(1).PaddingBottom(4).Text("Product").Bold();
                            header.Cell().BorderBottom(1).PaddingBottom(4).AlignRight().Text("Qty").Bold();
                            header.Cell().BorderBottom(1).PaddingBottom(4).AlignRight().Text("Unit price").Bold();
                            header.Cell().BorderBottom(1).PaddingBottom(4).AlignRight().Text("Line total").Bold();
                        });

                        foreach (var item in order.Items)
                        {
                            table.Cell().PaddingVertical(3).Text(item.ProductName);
                            table.Cell().PaddingVertical(3).AlignRight().Text(item.Quantity.ToString(Invariant));
                            table.Cell().PaddingVertical(3).AlignRight().Text(Money(order.Currency, item.UnitPrice));
                            table.Cell().PaddingVertical(3).AlignRight().Text(Money(order.Currency, item.LineTotal));
                        }
                    });

                    column.Item().AlignRight().Text($"Subtotal: {Money(order.Currency, order.Subtotal)}");
                    column.Item().AlignRight().Text($"Total: {Money(order.Currency, order.Total)}").Bold();

                    if (!string.IsNullOrWhiteSpace(order.CustomerNotes))
                    {
                        column.Item().Column(notes =>
                        {
                            notes.Item().Text("Customer notes").Bold();
                            notes.Item().Text(order.CustomerNotes);
                        });
                    }

                    column.Item().PaddingTop(10).Text(Terms).Italic();
                });

                page.Footer().AlignCenter().Text(text =>
                {
                    text.Span("Page ");
                    text.CurrentPageNumber();
                    text.Span(" of ");
                    text.TotalPages();
                });
            });
        }).GeneratePdf();
    }

    private static IEnumerable<string> CustomerLines(ShippingDetails? shipping, string? companyName, string customerEmail)
    {
        string?[] lines =
        [
            shipping?.FullName,
            companyName,
            customerEmail,
            shipping?.AddressLine,
            shipping?.City,
            shipping?.Province,
            shipping?.PostalCode
        ];

        return lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l!);
    }

    private static string Money(string currency, decimal amount) =>
        string.Create(Invariant, $"{currency} {amount:N2}");
}
