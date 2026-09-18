using SimmerAndSear.Contracts.Paging;

namespace SimmerAndSear.Contracts.Catalogue;
public enum ProductSort
{
    Relevance,
    NameAscending,
    NameDescending,
    PriceAscending,
    PriceDescending,
    Newest
}
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
