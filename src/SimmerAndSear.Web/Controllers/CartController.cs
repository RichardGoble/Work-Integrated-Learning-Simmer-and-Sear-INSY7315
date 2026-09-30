using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Views in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/views/overview?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public class CartController(
    CartService carts,
    CartSession session,
    OrderService orders,
    CurrentUser current,
    AuditWriter audit,
    IPaymentGateway gateway) : Controller
{
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        ViewData["Title"] = "Your Cart";

        
        var cart = await carts.ViewAsync(session.Resolve(), ct);

        ViewData["IsEmpty"] = cart.IsEmpty;
        return View(cart);
    }

    [HttpGet, Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> Checkout(CancellationToken ct)
    {
        ViewData["Title"] = "Checkout";

        var cart = await carts.ViewAsync(session.Resolve(), ct);

        if (cart.IsEmpty)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["Shipping"] = new CheckoutRequest();
        return View(cart);
    }

    [HttpPost, Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> Checkout(CheckoutRequest request, CancellationToken ct)
    {
        ViewData["Title"] = "Checkout";

        var cart = await carts.ViewAsync(session.Resolve(), ct);

        if (cart.IsEmpty)
        {
            return RedirectToAction(nameof(Index));
        }

        ViewData["Shipping"] = request;

        if (!ModelState.IsValid)
        {
            return View(cart);
        }

        var result = await orders.PlaceAsync(current.Id!, request, ct);

        switch (result.Outcome)
        {
            case CheckoutOutcome.EmptyCart:
                return RedirectToAction(nameof(Index));

            case CheckoutOutcome.LineUnavailable:
                ModelState.AddModelError(string.Empty, result.Detail ?? "Your cart needs attention.");
                return View(cart);
        }

        var order = result.Order!;

        await audit.WriteAsync(AuditAction.Create,
            $"{order.OrderType} {order.OrderReference} placed for {order.Total:F2} {order.Currency}");

    
        if (!order.IsQuotation && gateway.IsConfigured &&
            await orders.GetPaymentRequestAsync(order.Id, ct) is { } payment)
        {
            ViewData["HideNav"] = true;

            return View("Redirecting", gateway.BuildRedirect(
                payment,
                returnPath: $"/Cart/PaymentReturn/{order.Id}",
                cancelPath: $"/Cart/PaymentCancelled/{order.Id}",
                notifyPath: "/api/payments/notify"));
        }

        return RedirectToAction(nameof(OrderConfirmation), new { id = order.Id });
    }

    [HttpGet, Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> OrderConfirmation(string id, CancellationToken ct)
    {
        ViewData["Title"] = "Order Confirmed";


        var order = await orders.GetForClientAsync(id, current.Id!, ct);

        return order is null ? NotFound() : View(order);
    }

    [HttpGet("/Cart/PaymentReturn/{id}"), Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> PaymentReturn(string id, CancellationToken ct)
    {
        ViewData["Title"] = "Payment received";

        var order = await orders.GetForClientAsync(id, current.Id!, ct);

        return order is null ? NotFound() : View("OrderConfirmation", order);
    }

    [HttpGet("/Cart/PaymentCancelled/{id}"), Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> PaymentCancelled(string id, CancellationToken ct)
    {
        ViewData["Title"] = "Payment cancelled";

        var order = await orders.GetForClientAsync(id, current.Id!, ct);

        if (order is null)
        {
            return NotFound();
        }
        ViewData["OrderReference"] = order.OrderReference;
        return View(order);
    }
}
