namespace SimmerAndSear.Application.Abstractions;

//References
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 30 September 2026].

//Microsoft, 2026. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 30 September 2026].


public sealed record LookbookItem(
    string ProductName,
    string? Brand,
    string? Material,
    string? SizeLabel,
    decimal UnitPrice,
    bool InStock,
    byte[]? Image);

public sealed record Lookbook(
    string CustomerName,
    string Currency,
    DateTime GeneratedAt,
    IReadOnlyList<LookbookItem> Items)
{
    public decimal Total => Items.Sum(i => i.UnitPrice);
}

public interface IWishlistLookbookRenderer
{
    byte[] Render(Lookbook lookbook);
}
