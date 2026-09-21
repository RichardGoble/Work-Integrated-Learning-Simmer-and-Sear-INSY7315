using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Application.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public enum CheckoutOutcome
{
    Placed,
    EmptyCart,
    LineUnavailable
}


public sealed record CheckoutResult(CheckoutOutcome Outcome, OrderResponse? Order, string? Detail = null)
{
    public static CheckoutResult Placed(OrderResponse order) => new(CheckoutOutcome.Placed, order);

    public static CheckoutResult EmptyCart() => new(CheckoutOutcome.EmptyCart, null);

    public static CheckoutResult LineUnavailable(string detail) =>
        new(CheckoutOutcome.LineUnavailable, null, detail);
}


public sealed class OrderService(
    ICartRepository carts,
    IOrderRepository orders,
    IProductRepository products,
    ISiteSettingsProvider settings,
    OrderStrategyFactory strategies)
{
    public async Task<CheckoutResult> PlaceAsync(
        string clientId, CheckoutRequest request, CancellationToken ct = default)
    {
        var cart = await carts.GetByClientAsync(clientId, ct);

        if (cart is null || cart.IsEmpty)
        {
            return CheckoutResult.EmptyCart();
        }

        var site = await settings.CurrentAsync(ct);

        
        var catalogue = (await products.GetManyAsync(cart.Items.Select(i => i.ProductId), ct))
            .ToDictionary(p => p.Id);

        foreach (var line in cart.Items)
        {
            if (!catalogue.TryGetValue(line.ProductId, out var product) || !product.IsPublished)
            {
                return CheckoutResult.LineUnavailable($"{line.ProductName} is no longer available.");
            }

            if (product.StockQuantity < line.Quantity)
            {
                return CheckoutResult.LineUnavailable(
                    $"Only {product.StockQuantity} of {product.Name} remain. Please adjust your cart.");
            }

            line.ProductName = product.Name;
            line.UnitPrice = product.Price;
        }

        var strategy = strategies.For(cart, site);
        var order = strategy.CreateOrder(cart, clientId, ToShippingDetails(request), site);

        await orders.CreateAsync(order, ct);

        
        cart.Clear();
        await carts.SaveAsync(cart, ct);

        return CheckoutResult.Placed(Describe(order, strategy.NextStepMessage));
    }


    public async Task<OrderResponse?> GetForClientAsync(
        string orderId, string clientId, CancellationToken ct = default)
    {
        var order = await orders.GetForClientAsync(orderId, clientId, ct);

        if (order is null)
        {
            return null;
        }

        var site = await settings.CurrentAsync(ct);
        var strategy = order.OrderType == Domain.Enums.OrderType.QuoteRequest
            ? new QuotationFulfilmentStrategy().NextStepMessage
            : new PaymentFulfilmentStrategy().NextStepMessage;

        return Describe(order, strategy, site.DisplayCurrency);
    }

    private static ShippingDetails ToShippingDetails(CheckoutRequest request) => new()
    {
        FullName = request.Name.Trim(),
        Email = request.Email.Trim(),
        AddressLine = request.Address.Trim(),
        City = request.City.Trim(),
        PostalCode = request.Postal.Trim()
    };

    private static OrderResponse Describe(Order order, string nextStep, string? currency = null) =>
        new(
            order.Id,
            order.OrderReference,
            order.OrderType.ToString(),
            order.Status.ToString(),
            [.. order.Items.Select(i =>
                new OrderLineResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))],
            order.Subtotal,
            order.ShippingCost,
            order.Total,
            currency ?? order.Currency,
            order.ShippingDetails?.FullName ?? string.Empty,
            order.PlacedAt,
            order.OrderType == Domain.Enums.OrderType.QuoteRequest,
            nextStep);
}
