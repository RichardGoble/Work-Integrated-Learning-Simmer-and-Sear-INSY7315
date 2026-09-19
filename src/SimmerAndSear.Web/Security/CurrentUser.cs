using System.Security.Claims;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Use cookie authentication without ASP.NET Core Identity [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    public string? Id => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string Name => accessor.HttpContext?.User.Identity?.Name ?? "Anonymous";
    public RoleName? Role => Enum.TryParse<RoleName>(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
}
