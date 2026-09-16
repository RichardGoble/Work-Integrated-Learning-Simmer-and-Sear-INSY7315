namespace SimmerAndSear.Contracts.Paging;

/// <summary>
/// One page of results plus the total, so callers can render real page numbers
/// rather than an endless "load more".
/// </summary>
public sealed record Page<T>(IReadOnlyList<T> Items, long Total, int PageNumber, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public static Page<T> Empty(int pageNumber, int pageSize) =>
        new([], 0, pageNumber, pageSize);
}
