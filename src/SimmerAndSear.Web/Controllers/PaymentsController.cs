using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 25 September 2026].
//
//Microsoft, 2026. Options pattern [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options> [Accessed 25 September 2026].
//
//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 25 September 2026].
// ────────────────────────────────────────────────────────────────────────────

[ApiController, Route("api/orders")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class PaymentsController(
    OrderService orders,
    IPaymentGateway gateway,
    IOptions<PaymentOptions> options,
    AuditWriter audit,
    ILogger<PaymentsController> logger) : ControllerBase
{
    [HttpPost("{id}/simulate-payment")]
    [Authorize(Roles = nameof(RoleName.MasterAdmin))]
    public async Task<ActionResult<OrderResponse>> Simulate(string id, CancellationToken ct)
    {
        if (!options.Value.AllowSimulator)
        {
            return Problem(statusCode: 404, title: "Not found.");
        }

        var reference = $"SIM-{Guid.NewGuid().ToString("N")[..10].ToUpperInvariant()}";
        var pending = await orders.GetAsync(id, ct);

        if (pending is null)
        {
            return Problem(statusCode: 404, title: "That order could not be found.");
        }

        var result = await orders.ConfirmPaymentAsync(
            id, PaymentOptions.SimulatedProvider, reference, pending.Total, ct);

        switch (result.Outcome)
        {
            case PaymentOutcome.OrderNotFound:
                return Problem(statusCode: 404, title: "That order could not be found.");

            case PaymentOutcome.NotAwaitingPayment:
                return Problem(statusCode: 409,
                    title: "That order is not awaiting payment.");

            case PaymentOutcome.AmountMismatch:
                return Problem(statusCode: 409, title: "The amount does not match the order.");

            case PaymentOutcome.AlreadyConfirmed:
                return Ok(result.Order);
        }

        await audit.WriteAsync(AuditAction.Update, "Order", id,
            $"SIMULATED payment {reference} for {result.Order!.OrderReference} " +
            $"({result.Order.Total:F2} {result.Order.Currency})");

        if (result.HasStockShortfall)
        {
            logger.LogWarning(
                "Order {Reference} paid but stock could not be taken for: {Lines}",
                result.Order.OrderReference, string.Join(", ", result.StockShortfalls!));

            await audit.WriteAsync(AuditAction.Update, "Order", id,
                $"STOCK SHORTFALL on {result.Order.OrderReference}: " +
                string.Join(", ", result.StockShortfalls!));
        }

        return Ok(result.Order);
    }
    [HttpPost("/api/payments/notify")]
    [AllowAnonymous]
    [IgnoreAntiforgeryToken]
    public async Task<IActionResult> Notify(CancellationToken ct)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8);
        var rawBody = await reader.ReadToEndAsync(ct);

        var notification = await gateway.VerifyNotificationAsync(
            rawBody, HttpContext.Connection.RemoteIpAddress?.ToString(), ct);

        if (!notification.IsTrustworthy)
        {
            await audit.WriteAsync(AuditAction.AccessDenied, "Payment", null,
                $"ACCESS_DENIED payment notification refused: {notification.Rejection}");

            return Ok();
        }

        if (!notification.IsComplete)
        {
            logger.LogInformation(
                "Payment notification for {OrderId} reported {Status}; order left unpaid.",
                notification.OrderId, notification.PaymentStatus);

            return Ok();
        }

        var result = await orders.ConfirmPaymentAsync(
            notification.OrderId!, gateway.Provider,
            notification.GatewayReference ?? "unknown", notification.Amount, ct);

        switch (result.Outcome)
        {
            case PaymentOutcome.AlreadyConfirmed:
                return Ok();

            case PaymentOutcome.AmountMismatch:
                await audit.WriteAsync(AuditAction.AccessDenied, "Order", notification.OrderId,
                    $"ACCESS_DENIED payment of {notification.Amount:F2} does not match the order total");
                return Ok();

            case PaymentOutcome.OrderNotFound:
            case PaymentOutcome.NotAwaitingPayment:
                logger.LogWarning(
                    "Payment notification for {OrderId} could not be applied: {Outcome}.",
                    notification.OrderId, result.Outcome);
                return Ok();
        }

        await audit.WriteAsync(AuditAction.Update, "Order", notification.OrderId,
            $"Payment {notification.GatewayReference} received for {result.Order!.OrderReference} " +
            $"({result.Order.Total:F2} {result.Order.Currency})");

        if (result.HasStockShortfall)
        {
            logger.LogWarning("Order {Reference} paid but stock could not be taken for: {Lines}",
                result.Order.OrderReference, string.Join(", ", result.StockShortfalls!));

            await audit.WriteAsync(AuditAction.Update, "Order", notification.OrderId,
                $"STOCK SHORTFALL on {result.Order.OrderReference}: " +
                string.Join(", ", result.StockShortfalls!));
        }

        return Ok();
    }

}
