using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Customize the behavior of AuthorizationMiddleware [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/customizingauthorizationmiddlewareresponse?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//OWASP Foundation, n.d. Logging - OWASP Cheat Sheet Series [Online guide].
//Available at: <https://cheatsheetseries.owasp.org/cheatsheets/Logging_Cheat_Sheet.html> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class AuditedAuthorizationResultHandler : IAuthorizationMiddlewareResultHandler
{
    private readonly AuthorizationMiddlewareResultHandler fallback = new();

    public async Task HandleAsync(RequestDelegate next, HttpContext context,
        AuthorizationPolicy policy, PolicyAuthorizationResult result)
    {
        if (result.Challenged || result.Forbidden)
            await context.RequestServices.GetRequiredService<AuditWriter>().WriteAsync(
                AuditAction.AccessDenied, $"ACCESS_DENIED {context.Request.Method} {context.Request.Path}");
        await fallback.HandleAsync(next, context, policy, result);
    }
}
