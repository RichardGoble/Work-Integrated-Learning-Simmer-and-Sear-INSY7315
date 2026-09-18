using System.Security.Claims;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

public sealed class CurrentUser(IHttpContextAccessor accessor)
{
    public string? Id => accessor.HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier);
    public string Name => accessor.HttpContext?.User.Identity?.Name ?? "Anonymous";
    public RoleName? Role => Enum.TryParse<RoleName>(accessor.HttpContext?.User.FindFirstValue(ClaimTypes.Role), out var role) ? role : null;
}
