using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Handle errors in ASP.NET Core controller-based web APIs [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────


[ApiController, Route("api/cart")]
[AllowAnonymous]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class CartApiController(CartService carts, CartSession session) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<CartResponse>> Get(CancellationToken ct) =>
        Ok(await carts.ViewAsync(session.Resolve(), ct));

    [HttpPost("items")]
    public async Task<ActionResult<CartResponse>> Add(AddToCartRequest request, CancellationToken ct) =>
        Respond(await carts.AddAsync(session.ResolveOrIssue(), request.ProductId, request.Quantity, ct));

    [HttpPut("items/{productId}")]
    public async Task<ActionResult<CartResponse>> SetQuantity(
        string productId, UpdateCartItemRequest request, CancellationToken ct)
    {
        // No cookie and not signed in means there is no cart, so there is nothing to update.
        if (session.Resolve() is not { } owner)
        {
            return Problem(statusCode: 404, title: "Your cart is empty.");
        }

        return Respond(await carts.SetQuantityAsync(owner, productId, request.Quantity, ct));
    }

    [HttpDelete("items/{productId}")]
    public async Task<ActionResult<CartResponse>> Remove(string productId, CancellationToken ct)
    {
        if (session.Resolve() is not { } owner)
        {
            return Problem(statusCode: 404, title: "Your cart is empty.");
        }

        return Ok(await carts.RemoveAsync(owner, productId, ct));
    }

    [HttpDelete]
    public async Task<ActionResult<CartResponse>> Clear(CancellationToken ct)
    {
        if (session.Resolve() is not { } owner)
        {
            return Ok(await carts.ViewAsync(null, ct));
        }

        return Ok(await carts.ClearAsync(owner, ct));
    }

    /// <summary>
    /// Maps a cart outcome onto the status code that actually describes it: 404 for a
    /// product that is not on sale, 409 for one that is but cannot be supplied in the
    /// quantity asked for.
    /// </summary>
    private ActionResult<CartResponse> Respond(CartChangeResult result) => result.Outcome switch
    {
        CartChange.Updated => Ok(result.Cart),
        CartChange.ProductNotFound => Problem(statusCode: 404, title: "That product is no longer available."),
        _ => Problem(statusCode: 409, title: "There is not enough stock to fill that request.")
    };
}
