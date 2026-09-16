namespace SimmerAndSear.Contracts.Paging;

/// <summary>
/// Base for every query that reads a collection.
/// <para>
/// The page size is clamped in the setter rather than validated by a caller, so
/// an unbounded query is structurally impossible. That is both the performance
/// NFR and the main cost control: an accidental unlimited read is the single
/// most expensive mistake available in a document database.
/// </para>
/// </summary>
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
