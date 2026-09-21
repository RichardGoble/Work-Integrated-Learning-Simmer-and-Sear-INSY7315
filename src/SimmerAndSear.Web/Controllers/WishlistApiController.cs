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
public sealed class WishlistApiController(WishlistService service, CurrentUser current) : ControllerBase
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

    [HttpDelete]
    public async Task<IActionResult> Clear(CancellationToken ct)
    {
        await service.ClearAsync(current.Id!, ct);
        return NoContent();
    }
}
