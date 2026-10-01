using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Accounts;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Contracts.Paging;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;

namespace SimmerAndSear.Web.Controllers;

//References
//Microsoft, 2026. Create web APIs with ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Handle errors in ASP.NET Core controller-based web APIs [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/web-api/handle-errors?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 21 September 2026].

//OWASP Foundation, n.d. Mass Assignment - OWASP Cheat Sheet Series [Source code].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Mass_Assignment_Cheat_Sheet.html> [Accessed 21 September 2026].

[ApiController, Route("api/admin/accounts")]
[Authorize(Policy = nameof(Permission.AccountManage))]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountsAdminController(AdminAccountService service, CurrentUser current, AuditWriter audit) : ControllerBase
{
    private const string NotFoundTitle = "Admin account not found.";
    private const string DuplicateEmailTitle = "An account with this email already exists.";

    [HttpGet]
    public async Task<ActionResult<Page<AdminAccountResponse>>> List(
        [Range(1, int.MaxValue)] int pageNumber = 1,
        [Range(1, 100)] int pageSize = 20,
        CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);
        return Ok(await service.ListAsync(pageNumber, pageSize, ct));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<AdminAccountResponse>> Get(string id, CancellationToken ct)
    {
        var account = await service.GetAsync(id, ct);
        return account is null ? Problem(statusCode: 404, title: NotFoundTitle) : Ok(account);
    }

    [HttpPost("sales-admins")]
    public Task<ActionResult<AdminAccountResponse>> CreateSalesAdmin(CreateAdminAccountRequest request, CancellationToken ct) =>
        CreateAsync(request, RoleName.SalesAdmin, ct);

    [HttpPost("master-admins")]
    public Task<ActionResult<AdminAccountResponse>> CreateMasterAdmin(CreateAdminAccountRequest request, CancellationToken ct) =>
        CreateAsync(request, RoleName.MasterAdmin, ct);

    [HttpPut("{id}")]
    public async Task<ActionResult<AdminAccountResponse>> Update(string id, UpdateAdminAccountRequest request, CancellationToken ct)
    {
        var result = await service.UpdateAsync(id, request, ct);
        if (result.Outcome != AdminAccountOutcome.Ok) return OutcomeProblem(result.Outcome);

        await audit.WriteAsync(AuditAction.Update, "User", id, $"Admin account updated: {result.Account!.Email}");

        return Ok(result.Account);
    }

    [HttpPost("{id}/deactivate")]
    public Task<ActionResult<AdminAccountResponse>> Deactivate(string id, CancellationToken ct) =>
        SetActiveAsync(id, false, ct);

    [HttpPost("{id}/reactivate")]
    public Task<ActionResult<AdminAccountResponse>> Reactivate(string id, CancellationToken ct) =>
        SetActiveAsync(id, true, ct);

    private async Task<ActionResult<AdminAccountResponse>> CreateAsync(CreateAdminAccountRequest request, RoleName role, CancellationToken ct)
    {
        var result = await service.CreateAsync(request, role, ct);
        if (result.Outcome != AdminAccountOutcome.Ok) return OutcomeProblem(result.Outcome);

        var account = result.Account!;
        await audit.WriteAsync(AuditAction.Create, "User", account.Id, $"Admin account created: {account.Email} ({account.Role})");

        return Created($"/api/admin/accounts/{account.Id}", account);
    }

    private async Task<ActionResult<AdminAccountResponse>> SetActiveAsync(string id, bool active, CancellationToken ct)
    {
        var result = await service.SetActiveAsync(id, active, current.Id!, ct);
        if (result.Outcome != AdminAccountOutcome.Ok) return OutcomeProblem(result.Outcome);

        var verb = active ? "reactivated" : "deactivated";
        await audit.WriteAsync(AuditAction.Update, "User", id, $"Admin account {verb}: {result.Account!.Email}");

        return Ok(result.Account);
    }

    private ObjectResult OutcomeProblem(AdminAccountOutcome outcome) => outcome switch
    {
        AdminAccountOutcome.NotFound => Problem(statusCode: 404, title: NotFoundTitle),
        AdminAccountOutcome.DuplicateEmail => Problem(statusCode: 409, title: DuplicateEmailTitle),
        AdminAccountOutcome.CannotDeactivateSelf => Problem(statusCode: 400, title: "You cannot deactivate your own account."),
        _ => throw new ArgumentOutOfRangeException(nameof(outcome))
    };
}
