using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Accounts;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Domain.Enums;
using SimmerAndSear.Web.Security;
using Microsoft.AspNetCore.RateLimiting;

namespace SimmerAndSear.Web.Controllers;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Handle requests with controllers in ASP.NET Core MVC [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/mvc/controllers/actions?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Use cookie authentication without ASP.NET Core Identity [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Prevent Cross-Site Request Forgery (XSRF/CSRF) attacks in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
[ApiController, Route("api/accounts")]
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public sealed class AccountsController(AccountService accounts, AuditWriter audit, CurrentUser current) : ControllerBase
{
    [HttpGet("csrf"), AllowAnonymous]
    public IActionResult Csrf([FromServices] IAntiforgery antiforgery) =>
        Ok(new { token = antiforgery.GetAndStoreTokens(HttpContext).RequestToken });

    [HttpPost("register"), AllowAnonymous]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if (request.Password.Length < 12)
            return Problem(statusCode: 400, title: "Use a password of at least 12 characters.");
        var user = await accounts.RegisterAsync(request, ct);
        if (user is null) return Problem(statusCode: 400, title: "Unable to register with these details.");
        await audit.WriteAsync(AuditAction.Create, "Account registered", user);
        return StatusCode(201, new { message = "Account created. Sign in to continue." });
    }

    [HttpPost("login"), AllowAnonymous]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken ct)
    {
        var user = await accounts.LoginAsync(request, ct);
        if (user is null)
        {
            await audit.WriteAsync(AuditAction.AccessDenied, "ACCESS_DENIED login failed");
            return Problem(statusCode: 401, title: "Unable to sign in. Check your details or try again later.");
        }
        await audit.WriteAsync(AuditAction.Login, "Signed in", user);
        await HttpContext.SignInAsync(AccountCookies.CreatePrincipal(user));
        return Ok(new { user.Id, user.FullName, role = user.Role.ToString() });
    }

    [HttpPost("logout"), Authorize]
    public async Task<IActionResult> Logout()
    {
        await audit.WriteAsync(AuditAction.Logout, "Signed out");
        await HttpContext.SignOutAsync();
        return NoContent();
    }

    [HttpGet("me"), Authorize]
    public IActionResult Me() => Ok(new { current.Id, current.Name, role = current.Role?.ToString() });
}
