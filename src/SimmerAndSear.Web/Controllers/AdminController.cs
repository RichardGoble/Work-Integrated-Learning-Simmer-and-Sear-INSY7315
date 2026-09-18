using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace SimmerAndSear.Web.Controllers;

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
