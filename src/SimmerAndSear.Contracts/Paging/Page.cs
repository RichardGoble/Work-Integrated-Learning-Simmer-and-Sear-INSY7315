namespace SimmerAndSear.Contracts.Paging;
public sealed record Page<T>(IReadOnlyList<T> Items, long Total, int PageNumber, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public static Page<T> Empty(int pageNumber, int pageSize) =>
        new([], 0, pageNumber, pageSize);
}
