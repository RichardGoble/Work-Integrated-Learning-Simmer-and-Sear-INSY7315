using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Contracts.Catalogue;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Enumeration types - C# reference [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public enum ProductSort
{
    Relevance,
    NameAscending,
    NameDescending,
    PriceAscending,
    PriceDescending,
    Newest
}
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed record ProductFilter : PagedQuery
{
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public string? CookwareType { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    public ProductSort Sort { get; init; } = ProductSort.Relevance;
    public bool PublishedOnly { get; init; } = true;

    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);
}
