using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Commerce;

public sealed class OrderStrategyFactory
{
    private static readonly PaymentFulfilmentStrategy Payment = new();
    private static readonly QuotationFulfilmentStrategy Quotation = new();

    public IOrderFulfilmentStrategy For(Cart cart, SiteSettings settings) =>
        cart.ExceedsThreshold(settings.BulkOrderThreshold) ? Quotation : Payment;
}
