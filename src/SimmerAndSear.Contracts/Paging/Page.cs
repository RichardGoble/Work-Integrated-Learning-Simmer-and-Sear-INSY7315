namespace SimmerAndSear.Contracts.Paging;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. C# record types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/records> [Accessed 18 September 2026].
//
//Microsoft, 2025. Properties [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/programming-guide/classes-and-structs/properties> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed record Page<T>(IReadOnlyList<T> Items, long Total, int PageNumber, int PageSize)
{
    public int TotalPages => PageSize <= 0 ? 0 : (int)Math.Ceiling(Total / (double)PageSize);

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;

    public static Page<T> Empty(int pageNumber, int pageSize) =>
        new([], 0, pageNumber, pageSize);
}
