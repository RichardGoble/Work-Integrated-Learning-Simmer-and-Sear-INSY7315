using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

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
