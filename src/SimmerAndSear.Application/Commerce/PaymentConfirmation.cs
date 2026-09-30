using SimmerAndSear.Contracts.Commerce;

namespace SimmerAndSear.Application.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 25 September 2026].
//
//Microsoft, 2026. Handle errors in ASP.NET Core controller-based web APIs [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────

public enum PaymentOutcome
{
    Confirmed,
    AlreadyConfirmed,

    OrderNotFound,
    NotAwaitingPayment,
    AmountMismatch
}

public sealed record PaymentResult(
    PaymentOutcome Outcome,
    OrderResponse? Order = null,
    IReadOnlyList<string>? StockShortfalls = null)
{
    public bool HasStockShortfall => StockShortfalls is { Count: > 0 };

    public static PaymentResult Confirmed(OrderResponse order, IReadOnlyList<string> shortfalls) =>
        new(PaymentOutcome.Confirmed, order, shortfalls);

    public static PaymentResult AlreadyConfirmed(OrderResponse order) =>
        new(PaymentOutcome.AlreadyConfirmed, order);

    public static PaymentResult OrderNotFound() => new(PaymentOutcome.OrderNotFound);

    public static PaymentResult NotAwaitingPayment(OrderResponse order) =>
        new(PaymentOutcome.NotAwaitingPayment, order);

    public static PaymentResult AmountMismatch(OrderResponse order) =>
        new(PaymentOutcome.AmountMismatch, order);
}
