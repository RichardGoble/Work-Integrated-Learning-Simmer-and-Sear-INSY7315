namespace SimmerAndSear.Contracts.Paging;
public abstract record PagedQuery
{
    public const int MaxPageSize = 60;
    public const int DefaultPageSize = 24;

    private readonly int _pageNumber = 1;
    private readonly int _pageSize = DefaultPageSize;

    public int PageNumber
    {
        get => _pageNumber;
        init => _pageNumber = value < 1 ? 1 : value;
    }

    public int PageSize
    {
        get => _pageSize;
        init => _pageSize = Math.Clamp(value, 1, MaxPageSize);
    }

    public int Skip => (PageNumber - 1) * PageSize;
}
