using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Contracts.Paging;
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

    public async Task<OrderResponse?> GetAsync(string orderId, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(orderId, ct);
        return order is null ? null : Describe(order, PaidMessage);
    }

    /// <summary>
    /// Describes an order to a payment gateway, without handing it a domain entity.
    /// </summary>
    public async Task<PaymentRequest?> GetPaymentRequestAsync(
        string orderId, CancellationToken ct = default)
    {
        var order = await orders.GetAsync(orderId, ct);

        if (order is null)
        {
            return null;
        }

        var units = order.Items.Sum(i => i.Quantity);
        var lines = order.Items.Count;

        return new PaymentRequest(
            order.Id,
            order.OrderReference,
            order.Total,
            order.ShippingDetails?.FullName ?? string.Empty,
            order.ShippingDetails?.Email ?? string.Empty,
            $"{units} item{(units == 1 ? "" : "s")} across {lines} line{(lines == 1 ? "" : "s")}");
    }

    public async Task<PaymentResult> ConfirmPaymentAsync(
        string orderId, string provider, string gatewayReference, decimal amountPaid,
        CancellationToken ct = default)
    {
        var order = await orders.GetAsync(orderId, ct);

        if (order is null)
        {
            return PaymentResult.OrderNotFound();
        }

        if (order.IsAlreadyPaidBy(gatewayReference))
        {
            return PaymentResult.AlreadyConfirmed(Describe(order, PaidMessage));
        }

        if (!order.AwaitsPayment)
        {
            return PaymentResult.NotAwaitingPayment(Describe(order, PaidMessage));
        }

        if (decimal.Round(amountPaid, 2) != decimal.Round(order.Total, 2))
        {
            return PaymentResult.AmountMismatch(Describe(order, PaidMessage));
        }

        order.ConfirmPayment(provider, gatewayReference, amountPaid);
        await orders.UpdateAsync(order, ct);

        var shortfalls = await TakeStockAsync(order, ct);

        return PaymentResult.Confirmed(Describe(order, PaidMessage), shortfalls);
    }

    private async Task<IReadOnlyList<string>> TakeStockAsync(Order order, CancellationToken ct)
    {
        var shortfalls = new List<string>();

        foreach (var line in order.Items)
        {
            if (!await products.TryDecrementStockAsync(line.ProductId, line.Quantity, ct))
            {
                shortfalls.Add($"{line.ProductName} x{line.Quantity}");
            }
        }

        return shortfalls;
    }

    private const string PaidMessage = "Payment received. Your order is being prepared.";

    public async Task<OrderResponse?> GetForClientAsync(
        string orderId, string clientId, CancellationToken ct = default)
    {
        var order = await orders.GetForClientAsync(orderId, clientId, ct);

        if (order is null)
        {
            return null;
        }

        var site = await settings.CurrentAsync(ct);

        return Describe(order, NextStepFor(order), site.DisplayCurrency);
    }


    public async Task<Page<OrderResponse>> ListForClientAsync(
        string clientId, int pageNumber, int pageSize, CancellationToken ct = default)
    {
        var page = await orders.ListForClientAsync(clientId, pageNumber, pageSize, ct);
        var site = await settings.CurrentAsync(ct);

        return new Page<OrderResponse>(
            [.. page.Items.Select(o => Describe(o, NextStepFor(o), site.DisplayCurrency))],
            page.Total,
            page.PageNumber,
            page.PageSize);
    }

    private static string NextStepFor(Order order) =>
        order.OrderType == Domain.Enums.OrderType.QuoteRequest
            ? new QuotationFulfilmentStrategy().NextStepMessage
            : new PaymentFulfilmentStrategy().NextStepMessage;

    private static ShippingDetails ToShippingDetails(CheckoutRequest request) => new()
    {
        FullName = request.Name.Trim(),
        Email = request.Email.Trim(),
        AddressLine = request.Address.Trim(),
        City = request.City.Trim(),
        PostalCode = request.Postal.Trim()
    };

    internal static OrderResponse Describe(Order order, string nextStep, string? currency = null) =>
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
            nextStep,
            order.CompanyName,
            order.CustomerNotes);
}
