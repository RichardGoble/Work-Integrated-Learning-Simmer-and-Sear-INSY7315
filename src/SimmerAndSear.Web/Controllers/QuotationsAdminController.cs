using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Quotations;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Controllers;

//References
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, n.d. ControllerBase.File Method [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/api/microsoft.aspnetcore.mvc.controllerbase.file?view=aspnetcore-10.0> [Accessed 21 September 2026].

[ApiController, Route("api/admin/orders")]
[Authorize(Policy = nameof(Permission.InquiryManage))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class QuotationsAdminController(QuotationService service) : ControllerBase
{
    [HttpGet("{id}/quotation")]
    public async Task<IActionResult> Quotation(string id, CancellationToken ct)
    {
        var quotation = await service.GetForStaffAsync(id, ct);
        return quotation is null
            ? Problem(statusCode: 404, title: "Quotation not found.")
            : File(quotation.Content, "application/pdf", quotation.FileName);
    }
}
