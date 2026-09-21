using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Contracts.Wishlist;

//References
//Microsoft, 2026. Model validation in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/models/validation?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 21 September 2026].


public sealed class AddWishlistItemRequest
{
    [Required, StringLength(24, MinimumLength = 24)]
    public string ProductId { get; set; } = string.Empty;
}

public sealed record WishlistItemResponse(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    DateTime AddedAt);

public sealed record WishlistResponse(
    IReadOnlyList<WishlistItemResponse> Items,
    int Count);
