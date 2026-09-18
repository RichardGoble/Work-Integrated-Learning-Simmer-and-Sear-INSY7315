using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;

namespace SimmerAndSear.Web.Security;

public sealed class AccountCookies(IUserRepository users) : CookieAuthenticationEvents
{
    public static ClaimsPrincipal CreatePrincipal(User user) => new(new ClaimsIdentity(
    [
        new Claim(ClaimTypes.NameIdentifier, user.Id),
        new Claim(ClaimTypes.Name, user.FullName),
        new Claim(ClaimTypes.Email, user.Email),
        new Claim(ClaimTypes.Role, user.Role.ToString()),
        new Claim("credential_version", Fingerprint(user.PasswordHash))
    ], CookieAuthenticationDefaults.AuthenticationScheme));

    private static string Fingerprint(string hash) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(hash)));

    public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
    {
        var id = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
        var user = id is null ? null : await users.GetAsync(id, context.HttpContext.RequestAborted);
        if (user is null || !user.IsActive || user.IsLockedOut(DateTime.UtcNow) ||
            context.Principal?.FindFirstValue("credential_version") != Fingerprint(user.PasswordHash) ||
            context.Principal?.IsInRole(user.Role.ToString()) != true)
        {
            context.RejectPrincipal();
            await context.HttpContext.SignOutAsync();
            return;
        }
        context.ReplacePrincipal(CreatePrincipal(user));
    }

    public override Task RedirectToLogin(RedirectContext<CookieAuthenticationOptions> context)
    {
        if (context.Request.Path.StartsWithSegments("/api"))
            return Results.Problem(statusCode: 401, title: "Sign in required.").ExecuteAsync(context.HttpContext);
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    }

    public override Task RedirectToAccessDenied(RedirectContext<CookieAuthenticationOptions> context) =>
        Results.Problem(statusCode: 403, title: "You do not have permission to do that.").ExecuteAsync(context.HttpContext);
}
