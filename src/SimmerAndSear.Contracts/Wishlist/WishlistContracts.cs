using System.ComponentModel.DataAnnotations;
using SimmerAndSear.Contracts.Commerce;

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

public sealed class WishlistQuoteRequest
{
    [Required, StringLength(120, MinimumLength = 2)]
    public string Name { get; set; } = string.Empty;

    [Required, EmailAddress, StringLength(254)]
    public string Email { get; set; } = string.Empty;

    [StringLength(120)]
    public string? Company { get; set; }

    [StringLength(1000)]
    public string? Notes { get; set; }
}

public sealed record WishlistItemResponse(
    string ProductId,
    string ProductName,
    decimal UnitPrice,
    DateTime AddedAt,
    string? ImageId,
    string? ImageAlt,
    bool InStock,
    string? Brand = null,
    string? Material = null);

public sealed record WishlistResponse(
    IReadOnlyList<WishlistItemResponse> Items,
    int Count);

public sealed record MoveToCartResponse(
    WishlistResponse Wishlist,
    CartResponse Cart);
