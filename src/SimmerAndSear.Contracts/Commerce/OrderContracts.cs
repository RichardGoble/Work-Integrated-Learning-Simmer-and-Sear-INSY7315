using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 18 September 2026].
//



public sealed class CheckoutRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [Required, StringLength(200, MinimumLength = 3)]
    public string Address { get; set; } = string.Empty;

    [Required, StringLength(100, MinimumLength = 2)]
    public string City { get; set; } = string.Empty;

    [Required, StringLength(12, MinimumLength = 3)]
    public string Postal { get; set; } = string.Empty;
}

public sealed record OrderLineResponse(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal);


public sealed record OrderResponse(
    string Id,
    string OrderReference,
    string OrderType,
    string Status,
    IReadOnlyList<OrderLineResponse> Items,
    decimal Subtotal,
    decimal ShippingCost,
    decimal Total,
    string Currency,
    string CustomerName,
    DateTime PlacedAt,
    bool IsQuotation,
    string NextStepMessage,
    string? CompanyName = null,
    string? CustomerNotes = null);
