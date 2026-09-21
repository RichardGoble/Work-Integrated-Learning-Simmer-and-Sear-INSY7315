using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Contracts.Admin;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

[ApiController, Route("api/admin/settings")]
[Authorize(Policy = nameof(Permission.SettingsManage))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class SettingsController(SiteSettingsService service, CurrentUser current, AuditWriter audit) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<SiteSettingsResponse>> Get(CancellationToken ct) =>
        Ok(await service.GetAsync(ct));

    [HttpPut]
    public async Task<ActionResult<SiteSettingsResponse>> Update(
        UpdateSiteSettingsRequest request, CancellationToken ct)
    {
        var settings = await service.UpdateAsync(request, current.Name, ct);

        await audit.WriteAsync(AuditAction.Update, "SiteSettings", SiteSettings.SingletonId,
            $"Site settings updated by {current.Name}; BulkOrderThreshold={settings.BulkOrderThreshold}");

        return Ok(settings);
    }
}
