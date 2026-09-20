using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Accounts;
using SimmerAndSear.Contracts.Accounts;
using SimmerAndSear.Web.Security;
using SimmerAndSear.Web.ViewModels;
using SimmerAndSear.Domain.Enums;
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
[ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
public class AuthController(AccountService accounts, AuditWriter audit, CartClaim cartClaim) : Controller
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
        await cartClaim.OnSignedInAsync(user, ct);

   
        if (Url.IsLocalUrl(returnUrl))
        {
            return LocalRedirect(returnUrl!);
        }

        return Redirect(user.Role == RoleName.Customer ? "/" : "/Admin");
    }

    [HttpGet, AllowAnonymous]
    public IActionResult Register() => View(new RegisterForm());

    [HttpPost, AllowAnonymous]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<IActionResult> Register(RegisterForm form, CancellationToken ct)
    {
        if (!ModelState.IsValid) return View(form);

        var request = form.ToRequest();

        if (request is null)
        {
            ModelState.AddModelError(nameof(form.FullName), "Enter both your first and last name.");
            return View(form);
        }

        var user = await accounts.RegisterAsync(request, ct);
        if (user is null)
        {
            ModelState.AddModelError(string.Empty, "Unable to register with these details.");
            return View(form);
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
