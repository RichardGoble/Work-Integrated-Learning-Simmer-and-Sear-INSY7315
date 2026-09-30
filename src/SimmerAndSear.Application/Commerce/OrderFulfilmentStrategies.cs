using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Refactoring.Guru, n.d. Strategy Design Pattern [Online guide].
//Available at: <https://refactoring.guru/design-patterns/strategy> [Accessed 18 September 2026].
//
//Microsoft, 2026. C# classes [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/classes> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class PaymentFulfilmentStrategy : IOrderFulfilmentStrategy
{
    public string Name => "Payment";

    public Order CreateOrder(
        Cart cart, string clientId, ShippingDetails shipping, SiteSettings settings,
        string? companyName = null, string? customerNotes = null) =>
        Order.FromCart(cart, clientId, shipping, settings.ShippingFlatRate, settings.DisplayCurrency);

    public string NextStepMessage =>
        "Your order has been placed successfully.";
}

public sealed class QuotationFulfilmentStrategy : IOrderFulfilmentStrategy
{
    public string Name => "Quotation";

    public Order CreateOrder(
        Cart cart, string clientId, ShippingDetails shipping, SiteSettings settings,
        string? companyName = null, string? customerNotes = null) =>
        Order.FromQuoteRequest(cart, clientId, shipping, settings.DisplayCurrency, companyName, customerNotes);

    public string NextStepMessage =>
        "This is a bulk order, so a sales agent will contact you to confirm pricing, " +
        "delivery and any wholesale discount before payment is taken.";
}
