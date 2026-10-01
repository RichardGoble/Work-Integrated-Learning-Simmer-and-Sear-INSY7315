using SimmerAndSear.Contracts.Commerce;

namespace SimmerAndSear.Application.Wishlist;

//References
//Microsoft, 2025. Records [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record> [Accessed 21 September 2026].

//Microsoft, 2025. Enumeration types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/enum> [Accessed 21 September 2026].


public enum WishlistQuoteOutcome
{
    Placed,
    EmptyWishlist,
    LineUnavailable
}

public sealed record WishlistQuoteResult(WishlistQuoteOutcome Outcome, OrderResponse? Order, string? Detail = null);
