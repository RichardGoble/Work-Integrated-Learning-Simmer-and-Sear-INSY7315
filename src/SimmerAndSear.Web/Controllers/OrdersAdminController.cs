using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[ApiController, Route("api/admin/orders")]
[Authorize(Policy = nameof(Permission.InquiryManage))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersAdminController(IOrderRepository orders) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<OrderResponse>>> List(
        [Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        var page = await orders.ListAsync(pageNumber, pageSize, ct);
        return Ok(new Page<OrderResponse>(
            [.. page.Items.Select(o => new OrderResponse(
                o.Id,
                o.OrderReference,
                o.OrderType.ToString(),
                o.Status.ToString(),
                [.. o.Items.Select(i => new OrderLineResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))],
                o.Subtotal,
                o.ShippingCost,
                o.Total,
                o.Currency,
                o.ShippingDetails?.FullName ?? string.Empty,
                o.PlacedAt,
                o.OrderType == OrderType.QuoteRequest,
                string.Empty))],
            page.Total, page.PageNumber, page.PageSize));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> Get(string id, CancellationToken ct)
    {
        var order = await orders.GetAsync(id, ct);
        if (order is null) return Problem(statusCode: 404, title: "Order not found.");

        return Ok(new OrderResponse(
            order.Id,
            order.OrderReference,
            order.OrderType.ToString(),
            order.Status.ToString(),
            [.. order.Items.Select(i => new OrderLineResponse(i.ProductId, i.ProductName, i.UnitPrice, i.Quantity, i.LineTotal))],
            order.Subtotal,
            order.ShippingCost,
            order.Total,
            order.Currency,
            order.ShippingDetails?.FullName ?? string.Empty,
            order.PlacedAt,
            order.OrderType == OrderType.QuoteRequest,
            string.Empty));
    }
}
