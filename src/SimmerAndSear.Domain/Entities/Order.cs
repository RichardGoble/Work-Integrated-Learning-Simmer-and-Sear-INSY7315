using SimmerAndSear.Domain.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Domain.Entities;

public sealed class Order : IEntity
{
    public string Id { get; set; } = string.Empty;

    public string OrderReference { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public OrderType OrderType { get; set; }
    public OrderStatus Status { get; set; }

    public List<OrderItem> Items { get; set; } = [];

    public decimal Subtotal { get; set; }
    public decimal ShippingCost { get; set; }
    public decimal Total { get; set; }
    public string Currency { get; set; } = "ZAR";

    public ShippingDetails? ShippingDetails { get; set; }
    public Payment? Payment { get; set; }

    public string? CompanyName { get; set; }
    public string? CustomerNotes { get; set; }

    public DateTime PlacedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public static Order FromCart(
        Cart cart, string clientId, ShippingDetails shipping, decimal shippingCost, string currency = "ZAR")
    {
        var order = Build(cart, clientId, currency);

        order.OrderType = OrderType.Checkout;
        order.Status = OrderStatus.AwaitingPayment;
        order.ShippingDetails = shipping;
        order.ShippingCost = shippingCost;
        order.Total = order.Subtotal + shippingCost;

        return order;
    }

    public static Order FromQuoteRequest(
        Cart cart, string clientId, ShippingDetails? shipping = null, string currency = "ZAR",
        string? companyName = null, string? customerNotes = null)
    {
        var order = Build(cart, clientId, currency);

        order.OrderType = OrderType.QuoteRequest;
        order.Status = OrderStatus.Pending;
        order.ShippingDetails = shipping;
        order.ShippingCost = 0m;
        order.Total = order.Subtotal;
        order.CompanyName = companyName;
        order.CustomerNotes = customerNotes;

        return order;
    }

    public static Order FromWishlistQuote(
        IEnumerable<OrderItem> items, string clientId, ShippingDetails contact,
        string? companyName, string? customerNotes, string currency = "ZAR")
    {
        var order = Build(items, clientId, currency);

        order.OrderType = OrderType.QuoteRequest;
        order.Status = OrderStatus.Pending;
        order.ShippingDetails = contact;
        order.ShippingCost = 0m;
        order.Total = order.Subtotal;
        order.CompanyName = companyName;
        order.CustomerNotes = customerNotes;

        return order;
    }

    private static Order Build(Cart cart, string clientId, string currency) =>
        Build(cart.Items.Select(i => new OrderItem
        {
            ProductId = i.ProductId,
            ProductName = i.ProductName,
            UnitPrice = i.UnitPrice,
            Quantity = i.Quantity
        }), clientId, currency);

    private static Order Build(IEnumerable<OrderItem> items, string clientId, string currency)
    {
        var now = DateTime.UtcNow;

        var order = new Order
        {
            ClientId = clientId,
            Currency = currency,
            PlacedAt = now,
            UpdatedAt = now,
            OrderReference = BuildReference(now),
            Items = [.. items]
        };

        order.Subtotal = order.Items.Sum(i => i.LineTotal);
        return order;
    }

    private static string BuildReference(DateTime utcNow) =>
        $"SS-{utcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}";

    public bool HasLines => Items.Count > 0;

    public bool AwaitsPayment => OrderType == OrderType.Checkout && Status == OrderStatus.AwaitingPayment;

    public bool IsAlreadyPaidBy(string gatewayReference) =>
        Status == OrderStatus.Paid &&
        Payment is not null &&
        string.Equals(Payment.GatewayReference, gatewayReference, StringComparison.Ordinal);


    public void ConfirmPayment(string provider, string gatewayReference, decimal amount)
    {
        Payment = new Payment
        {
            Provider = provider,
            GatewayReference = gatewayReference,
            Amount = amount,
            Status = PaymentStatus.Confirmed,
            PaidAt = DateTime.UtcNow
        };

        Status = OrderStatus.Paid;
        UpdatedAt = DateTime.UtcNow;
    }
}

public sealed class OrderItem
{
    public string ProductId { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
}

public sealed class ShippingDetails
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string? Province { get; set; }
}

public sealed class Payment
{
    public string PaymentId { get; set; } = Guid.NewGuid().ToString("N");
    public string Provider { get; set; } = "PAYFAST";
    public string? GatewayReference { get; set; }
    public decimal Amount { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.Initiated;
    public DateTime? PaidAt { get; set; }
}
