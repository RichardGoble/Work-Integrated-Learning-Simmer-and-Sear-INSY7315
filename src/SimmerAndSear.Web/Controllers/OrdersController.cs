using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Commerce;
using SimmerAndSear.Application.Quotations;
using SimmerAndSear.Contracts.Commerce;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

//References
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, n.d. ControllerBase.File Method [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.file?view=aspnetcore-10.0> [Accessed 21 September 2026].

//OWASP, n.d. Insecure Direct Object Reference Prevention Cheat Sheet [Source code].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Insecure_Direct_Object_Reference_Prevention_Cheat_Sheet.html> [Accessed 21 September 2026].

[ApiController, Route("api/orders")]
[Authorize(Policy = Permissions.Customer)]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class OrdersController(OrderService service, QuotationService quotations, CurrentUser current) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<Page<OrderResponse>>> List(
        [Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        return Ok(await service.ListForClientAsync(current.Id!, pageNumber, pageSize, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<OrderResponse>> Get(string id, CancellationToken ct)
    {
        var order = await service.GetForClientAsync(id, current.Id!, ct);
        return order is null ? Problem(statusCode: 404, title: "Order not found.") : Ok(order);
    }

    [HttpGet("{id}/quotation")]
    public async Task<IActionResult> Quotation(string id, CancellationToken ct)
    {
        var quotation = await quotations.GetForClientAsync(id, current.Id!, ct);
        return quotation is null
            ? Problem(statusCode: 404, title: "Quotation not found.")
            : File(quotation.Content, "application/pdf", quotation.FileName);
    }
}
