 using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public sealed class AddToCartRequest
{
    [Required, StringLength(64, MinimumLength = 1)]
    public string ProductId { get; set; } = string.Empty;

    [Range(1, 999)]
    public int Quantity { get; set; } = 1;
}


public sealed class UpdateCartItemRequest
{
    [Range(0, 999)]
    public int Quantity { get; set; }
}

public sealed record CartLineResponse(
    string ProductId,
    string ProductName,
    string Brand,
    string? Material,
    string? ImageId,
    string? ImageAlt,
    decimal UnitPrice,
    int Quantity,
    decimal LineTotal,
    int StockQuantity,
    bool IsAvailable);

public sealed record CartResponse(
    IReadOnlyList<CartLineResponse> Items,
    int TotalQuantity,
    decimal Subtotal,
    decimal ShippingCost,
    string Currency,
    decimal BulkOrderThreshold,
    bool RequiresQuotation)
{
    public bool IsEmpty => Items.Count == 0;

    
    public decimal Total => Subtotal + ShippingCost;

    
    public bool HasUnavailableLines => Items.Any(i => !i.IsAvailable);
}
