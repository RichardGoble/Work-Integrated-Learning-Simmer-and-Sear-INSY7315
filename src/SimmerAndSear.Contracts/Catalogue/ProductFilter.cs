using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Contracts.Catalogue;

/// <summary>How a catalogue result set is ordered. Each value maps to an indexed field.</summary>
public enum ProductSort
{
    Relevance,
    NameAscending,
    NameDescending,
    PriceAscending,
    PriceDescending,
    Newest
}

/// <summary>
/// Data Transfer Object carrying the catalogue query from the request to the
/// service layer. Bound from the query string, validated, and never a domain
/// entity - request types are always separate so that no caller can smuggle a
/// field the model was not meant to accept.
/// </summary>
public sealed record ProductFilter : PagedQuery
{
    public string? SearchTerm { get; init; }
    public string? Category { get; init; }
    public string? Brand { get; init; }
    public string? CookwareType { get; init; }
    public decimal? MinPrice { get; init; }
    public decimal? MaxPrice { get; init; }

    public ProductSort Sort { get; init; } = ProductSort.Relevance;

    /// <summary>
    /// Anonymous visitors see published listings only. Set by the service layer
    /// from the caller's role, never bound from the request.
    /// </summary>
    public bool PublishedOnly { get; init; } = true;

    public bool HasSearchTerm => !string.IsNullOrWhiteSpace(SearchTerm);
}
