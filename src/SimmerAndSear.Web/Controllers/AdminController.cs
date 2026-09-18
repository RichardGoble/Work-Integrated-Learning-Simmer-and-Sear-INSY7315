using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Enums;
using System.ComponentModel.DataAnnotations;

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
[Authorize(Policy = Permissions.Staff)]
public class AdminController(IAuditLogRepository audit) : Controller
{
    public IActionResult Index()
    {
        ViewData["Title"] = "Admin Dashboard";
        return View();
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login()
    {
        return RedirectToAction("Login", "Auth", new { returnUrl = "/Admin" });
    }

    [HttpGet, Authorize(Policy = nameof(Permission.AuditRead))]
    public async Task<IActionResult> Audit([Range(1, int.MaxValue)] int pageNumber = 1, CancellationToken ct = default)
    {
        if (!ModelState.IsValid) return BadRequest();
        return View(await audit.ListAsync(pageNumber, 20, ct));
    }
}
