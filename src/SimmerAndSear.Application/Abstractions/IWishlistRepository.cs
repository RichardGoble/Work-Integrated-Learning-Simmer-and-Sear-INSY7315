namespace SimmerAndSear.Application.Abstractions;
//References
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].

//Microsoft, 2025. Asynchronous programming [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/asynchronous-programming/> [Accessed 21 September 2026].


public interface IWishlistRepository
{
    Task<Domain.Entities.Wishlist?> GetByClientAsync(string clientId, CancellationToken ct = default);

    Task SaveAsync(Domain.Entities.Wishlist wishlist, CancellationToken ct = default);
}
