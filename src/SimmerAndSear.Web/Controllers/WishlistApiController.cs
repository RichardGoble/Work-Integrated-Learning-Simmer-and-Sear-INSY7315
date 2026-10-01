using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Wishlist;
using SimmerAndSear.Contracts.Wishlist;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

//References
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Handle errors in ASP.NET Core controller-based web APIs [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 21 September 2026].


[ApiController, Route("api/wishlist")]
[Authorize(Policy = Permissions.Customer)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class WishlistApiController(
    WishlistService service, LookbookService lookbooks, CurrentUser current) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<WishlistResponse>> Get(CancellationToken ct) =>
        Ok(await service.GetAsync(current.Id!, ct));

    [HttpPost("items")]
    public async Task<ActionResult<WishlistResponse>> Add(AddWishlistItemRequest request, CancellationToken ct)
    {
        var (wishlist, error) = await service.AddAsync(current.Id!, request.ProductId, ct);
        return error is not null ? Problem(statusCode: 400, title: error) : Ok(wishlist);
    }

    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<WishlistResponse>> Remove(string productId, CancellationToken ct) =>
        Ok(await service.RemoveAsync(current.Id!, productId, ct));

    [HttpPost("items/{productId}/move-to-cart")]
    public async Task<ActionResult<MoveToCartResponse>> MoveToCart(string productId, CancellationToken ct)
    {
        var result = await service.MoveToCartAsync(current.Id!, productId, ct);

        return result.Outcome switch
        {
            WishlistMoveOutcome.Moved => Ok(result.Result),
            WishlistMoveOutcome.NotInWishlist => Problem(statusCode: 404, title: "That product is not in your wishlist."),
            WishlistMoveOutcome.Unavailable => Problem(statusCode: 404, title: "That product is no longer available."),
            _ => Problem(statusCode: 409, title: "There is not enough stock to fill that request.")
        };
    }

    [HttpPost("quote-request")]
    public async Task<IActionResult> RequestQuote(WishlistQuoteRequest request, CancellationToken ct)
    {
        var result = await service.RequestQuoteAsync(current.Id!, request, ct);

        return result.Outcome switch
        {
            WishlistQuoteOutcome.Placed => Created($"/api/orders/{result.Order!.Id}", result.Order),
            WishlistQuoteOutcome.EmptyWishlist => Problem(statusCode: 400, title: "Your wishlist is empty."),
            _ => Problem(statusCode: 409, title: result.Detail)
        };
    }

    [HttpGet("lookbook")]
    public async Task<IActionResult> Lookbook(CancellationToken ct)
    {
        var lookbook = await lookbooks.BuildAsync(current.Id!, User.Identity?.Name ?? string.Empty, ct);

        return lookbook is null
            ? Problem(statusCode: 400, title: "Add something to your wishlist before downloading a lookbook.")
            : File(lookbook.Content, "application/pdf", lookbook.FileName);
    }

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await service.ClearAsync(current.Id!, ct);
        return NoContent();
    }
}
