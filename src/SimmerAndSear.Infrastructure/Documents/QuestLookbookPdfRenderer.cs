using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SimmerAndSear.Application.Abstractions;

namespace SimmerAndSear.Infrastructure.Documents;

//References
//QuestPDF, n.d. Getting started [Source code].
//Available at: <https://www.questpdf.com/quick-start.html> [Accessed 30 September 2026].

//Microsoft, 2026. Standard numeric format strings [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/standard/base-types/standard-numeric-format-strings> [Accessed 30 September 2026].


public sealed class QuestLookbookPdfRenderer : IWishlistLookbookRenderer
{
    private static readonly CultureInfo Invariant = CultureInfo.InvariantCulture;

    private const int Columns = 2;

    static QuestLookbookPdfRenderer()
    {
        QuestPDF.Settings.License = LicenseType.Community;
    }

    public byte[] Render(Lookbook lookbook)
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
                        row.RelativeItem().AlignRight().Text("LOOKBOOK").FontSize(20).Bold();
                    });

                    if (!string.IsNullOrWhiteSpace(lookbook.CustomerName))
                    {
                        column.Item().PaddingTop(5).Text($"Selected by {lookbook.CustomerName}");
                    }

                    column.Item().Text(
                        $"Prepared {lookbook.GeneratedAt.ToString("d MMMM yyyy", Invariant)}");
                    column.Item().PaddingBottom(5).Text(
                        $"{lookbook.Items.Count} item{(lookbook.Items.Count == 1 ? "" : "s")}");
                });

                page.Content().PaddingVertical(15).Column(column =>
                {
                    column.Spacing(15);

                    column.Item().Table(table =>
                    {
                        table.ColumnsDefinition(columns =>
                        {
                            for (var i = 0; i < Columns; i++)
                            {
                                columns.RelativeColumn();
                            }
                        });

                        foreach (var item in lookbook.Items)
                        {
                            table.Cell().Padding(6).Column(cell =>
                            {
               
                                if (item.Image is { Length: > 0 })
                                {
                                    cell.Item()
                                        .Height(150)
                                        .AlignCenter()
                                        .Image(item.Image)
                                        .FitArea();
                                }

                                cell.Item().PaddingTop(6).Text(item.ProductName).Bold();

                                foreach (var line in Details(item))
                                {
                                    cell.Item().Text(line).FontSize(9).FontColor(Colors.Grey.Darken2);
                                }

                                cell.Item().PaddingTop(3)
                                    .Text(Money(lookbook.Currency, item.UnitPrice)).Bold();

                                if (!item.InStock)
                                {
                                    cell.Item().Text("Out of stock").FontSize(9).Italic();
                                }
                            });
                        }
                    });

                    column.Item().PaddingTop(10).AlignRight()
                        .Text($"Indicative total: {Money(lookbook.Currency, lookbook.Total)}").Bold();

            
                    column.Item().Text(
                        "This lookbook is a selection of products, not a quotation. Prices are " +
                        "indicative and exclude delivery. Request a quotation for firm pricing.")
                        .FontSize(9).Italic();
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

    private static IEnumerable<string> Details(LookbookItem item)
    {
        string?[] lines = [item.Brand, item.Material, item.SizeLabel];

        return lines.Where(l => !string.IsNullOrWhiteSpace(l)).Select(l => l!);
    }

    private static string Money(string currency, decimal amount) =>
        $"{currency} {amount.ToString("N2", Invariant)}";
}
