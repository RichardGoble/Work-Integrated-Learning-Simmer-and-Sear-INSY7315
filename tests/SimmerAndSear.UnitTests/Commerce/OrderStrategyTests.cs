using Shouldly;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.UnitTests.Commerce;


public sealed class OrderStrategyTests
{
    private const decimal Threshold = 5000m;

    private static readonly SiteSettings Settings = new()
    {
        BulkOrderThreshold = Threshold,
        ShippingFlatRate = 120m,
        DisplayCurrency = "ZAR"
    };

    private static readonly ShippingDetails Shipping = new()
    {
        FullName = "Thandi Mokoena",
        Email = "thandi@restaurant.co.za",
        AddressLine = "14 Long Street",
        City = "Cape Town",
        PostalCode = "8001"
    };

    private static Cart CartOf(decimal unitPrice, int quantity)
    {
        var cart = new Cart();
        cart.AddItem("product-1", "Heritage Skillet 26cm", unitPrice, quantity);
        return cart;
    }

    [Theory]
    [InlineData(4999, "Payment")]
    [InlineData(5000, "Quotation")]
    [InlineData(5001, "Quotation")]
    public void Factory_branches_at_the_threshold(decimal subtotal, string expected) =>
        new OrderStrategyFactory().For(CartOf(subtotal, 1), Settings).Name.ShouldBe(expected);

    [Fact]
    public void Quotation_carries_the_company_and_notes_onto_the_order()
    {
        var order = new QuotationFulfilmentStrategy().CreateOrder(
            CartOf(3000m, 4), "client-1", Shipping, Settings,
            companyName: "Mokoena Restaurant Group",
            customerNotes: "Delivery to the Long Street kitchen before 10am.");

        order.OrderType.ShouldBe(OrderType.QuoteRequest);
        order.CompanyName.ShouldBe("Mokoena Restaurant Group");
        order.CustomerNotes.ShouldBe("Delivery to the Long Street kitchen before 10am.");


        order.ShippingCost.ShouldBe(0m);
        order.Total.ShouldBe(order.Subtotal);
    }

    [Fact]
    public void Quotation_without_a_company_leaves_it_unset()
    {
        var order = new QuotationFulfilmentStrategy().CreateOrder(
            CartOf(6000m, 1), "client-1", Shipping, Settings);

        order.CompanyName.ShouldBeNull();
        order.CustomerNotes.ShouldBeNull();
    }

    [Fact]
    public void Payment_ignores_the_bulk_fields_and_charges_shipping()
    {
        var order = new PaymentFulfilmentStrategy().CreateOrder(
            CartOf(500m, 2), "client-1", Shipping, Settings,
            companyName: "Should not appear",
            customerNotes: "Should not appear");

        order.OrderType.ShouldBe(OrderType.Checkout);
        order.CompanyName.ShouldBeNull();
        order.CustomerNotes.ShouldBeNull();
        order.ShippingCost.ShouldBe(120m);
        order.Total.ShouldBe(1120m);
    }
}
