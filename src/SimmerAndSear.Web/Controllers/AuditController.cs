using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Controllers;

[ApiController, Route("api/admin/audit")]
[Authorize(Policy = nameof(Permission.AuditRead))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AuditController(IAuditLogRepository audit) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> List([Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20, CancellationToken ct = default) =>
        Ok(await audit.ListAsync(pageNumber, pageSize, ct));
}
