using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimmerAndSear.Application.Accounts;
using SimmerAndSear.Contracts.Accounts;
using System.Security.Claims;
using SimmerAndSear.Application.Admin;
using SimmerAndSear.Application.Commerce;
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
public class AuthController(
    AccountService accounts,
    AuditWriter audit,
    CartClaim cartClaim,
    CurrentUser current,
    OrderService orderService,
    ReviewService reviewService) : Controller
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
    public async Task<IActionResult> Profile(CancellationToken ct)
    {
        ViewData["Title"] = "My Profile";
        return View(await BuildProfileAsync(ct));
    }

    [HttpPost, Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> UpdateEmail(string email, CancellationToken ct)
    {
        var error = await accounts.ChangeEmailAsync(current.Id!, email ?? string.Empty, ct);

        if (error is null)
        {
            await audit.WriteAsync(AuditAction.Update, "Account changed email address");


            TempData["ProfileMessage"] = "Your email address has been updated.";
        }
        else
        {
            TempData["ProfileError"] = error;
        }

        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, Authorize(Policy = Permissions.Customer)]
    [EnableRateLimiting(SecurityHardening.AccountPolicy)]
    public async Task<IActionResult> UpdatePassword(
        string currentPassword, string newPassword, CancellationToken ct)
    {
        var error = await accounts.ChangePasswordAsync(
            current.Id!, currentPassword ?? string.Empty, newPassword ?? string.Empty, ct);

        if (error is null)
        {
            await audit.WriteAsync(AuditAction.Update, "Account changed password");

     
            await HttpContext.SignOutAsync();
            TempData["ProfileMessage"] = "Your password has been changed. Please sign in again.";
            return RedirectToAction(nameof(Login));
        }

        TempData["ProfileError"] = error;
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, Authorize(Policy = Permissions.Customer)]
    public async Task<IActionResult> Deactivate(CancellationToken ct)
    {
        var error = await accounts.DeactivateAsync(current.Id!, ct);

        if (error is not null)
        {
            TempData["ProfileError"] = error;
            return RedirectToAction(nameof(Profile));
        }

        await audit.WriteAsync(AuditAction.Update, "Account deactivated by its owner");
        await HttpContext.SignOutAsync();

        return RedirectToAction("Index", "Home");
    }

    private async Task<ProfileVm> BuildProfileAsync(CancellationToken ct)
    {
        var name = User.Identity?.Name ?? string.Empty;
        var email = User.FindFirstValue(ClaimTypes.Email) ?? string.Empty;
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;

   
        if (role != nameof(RoleName.Customer) || current.Id is not { Length: > 0 } clientId)
        {
            return ProfileVm.Empty(name, email, role);
        }

        var orders = await orderService.ListForClientAsync(clientId, 1, 20, ct);
        var reviews = await reviewService.ListForClientAsync(clientId, 1, 20, ct);

        return new ProfileVm(name, email, role, orders.Items, reviews.Items);
    }
}
