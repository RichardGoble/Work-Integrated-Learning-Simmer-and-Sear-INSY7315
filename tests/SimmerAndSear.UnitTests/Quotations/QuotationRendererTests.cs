using Shouldly;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Documents;

namespace SimmerAndSear.UnitTests.Quotations;


public sealed class QuotationRendererTests
{
    private static readonly DateTime ValidUntil = new(2026, 10, 30, 0, 0, 0, DateTimeKind.Utc);

    private static Order QuoteOrder(string? companyName, string? customerNotes)
    {
        var cart = new Cart();
        cart.AddItem("product-1", "Signature Tri-Ply 5-Piece Set", 3499m, 2);

        return Order.FromQuoteRequest(
            cart,
            "client-1",
            new ShippingDetails
            {
                FullName = "Thandi Mokoena",
                Email = "thandi@restaurant.co.za",
                AddressLine = "14 Long Street",
                City = "Cape Town",
                PostalCode = "8001"
            },
            "ZAR",
            companyName,
            customerNotes);
    }

    [Fact]
    public void Renders_a_pdf()
    {
        var bytes = new QuestQuotationPdfRenderer()
            .Render(QuoteOrder(null, null), "thandi@restaurant.co.za", ValidUntil);

        bytes.Length.ShouldBeGreaterThan(0);
        System.Text.Encoding.ASCII.GetString(bytes, 0, 5).ShouldBe("%PDF-");
    }

    [Fact]
    public void Company_and_notes_change_the_document()
    {
        var renderer = new QuestQuotationPdfRenderer();

        var plain = renderer.Render(
            QuoteOrder(null, null), "thandi@restaurant.co.za", ValidUntil);

        var detailed = renderer.Render(
            QuoteOrder("Mokoena Restaurant Group", "Delivery to the Long Street kitchen before 10am."),
            "thandi@restaurant.co.za",
            ValidUntil);

        detailed.ShouldNotBe(plain);
        detailed.Length.ShouldBeGreaterThan(plain.Length);
    }
}
