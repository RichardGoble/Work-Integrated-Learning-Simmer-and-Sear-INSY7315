using SimmerAndSear.Contracts.Commerce;

namespace SimmerAndSear.Application.Commerce;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 18 September 2026].
//
//Microsoft, 2026. Handle errors in ASP.NET Core controller-based web APIs [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


public enum CartChange
{
    Updated,
    ProductNotFound,
    OutOfStock
}


public sealed record CartChangeResult(CartChange Outcome, CartResponse? Cart)
{
    public static CartChangeResult Updated(CartResponse cart) => new(CartChange.Updated, cart);

    public static CartChangeResult ProductNotFound() => new(CartChange.ProductNotFound, null);

    public static CartChangeResult OutOfStock() => new(CartChange.OutOfStock, null);
}
