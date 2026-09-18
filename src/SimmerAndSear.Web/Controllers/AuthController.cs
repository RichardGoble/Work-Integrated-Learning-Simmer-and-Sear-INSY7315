using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Accounts;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Domain.Enums;
using Microsoft.AspNetCore.RateLimiting;

namespace SimmerAndSear.Web.Controllers;

[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(AccountService accounts, AuditWriter audit) : Controller
{
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        return View(new LoginRequest());
    }

    [HttpPost, AllowAnonymous]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<IActionResult> Login(LoginRequest request, string? returnUrl, CancellationToken ct)
    {
        ViewData["ReturnUrl"] = Url.IsLocalUrl(returnUrl) ? returnUrl : null;
        if (!ModelState.IsValid) return View(request);
        var user = await accounts.LoginAsync(request, ct);
        if (user is null)
        {
            await audit.WriteAsync(AuditAction.AccessDenied, "ACCESS_DENIED login failed");
            ModelState.AddModelError(string.Empty, "Unable to sign in. Check your details or try again later.");
            return View(request);
        }
        await audit.WriteAsync(AuditAction.Login, "Signed in", user);
        await HttpContext.SignInAsync(AccountCookies.CreatePrincipal(user));
        return LocalRedirect(Url.IsLocalUrl(returnUrl) ? returnUrl! : "/Auth/Profile");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterRequest());

    [HttpPost, AllowAnonymous]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<IActionResult> Register(RegisterRequest request, CancellationToken ct)
    {
        if ((request.Password?.Length ?? 0) < 12) ModelState.AddModelError(nameof(request.Password), "Use at least 12 characters.");
        if (!ModelState.IsValid) return View(request);
        var user = await accounts.RegisterAsync(request, ct);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Unable to register with these details.");
            return View(request);
        }
        await audit.WriteAsync(AuditAction.Create, "Account registered", user);
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, Authorize]
    public async Task<IActionResult> Logout()
    {
        await audit.WriteAsync(AuditAction.Logout, "Signed out");
        await HttpContext.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    [HttpGet, Authorize]
    public IActionResult Profile() => View();
}
