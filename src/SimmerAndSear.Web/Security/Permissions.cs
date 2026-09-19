using Microsoft.AspNetCore.Authorization;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Policy-based authorization in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authorization/policies?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public static class Permissions
{
    public const string Customer = "Customer";
    public const string Staff = "Staff";

    public static IServiceCollection AddAccountAuthorization(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<CurrentUser>();
        services.AddScoped<AuditWriter>();
        services.AddSingleton<IAuthorizationMiddlewareResultHandler, AuditedAuthorizationResultHandler>();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(Customer, policy => policy.RequireAuthenticatedUser().RequireRole(nameof(RoleName.Customer)));
            options.AddPolicy(Staff, policy => policy.RequireAuthenticatedUser().RequireRole(nameof(RoleName.SalesAdmin), nameof(RoleName.MasterAdmin)));
            foreach (var permission in Enum.GetValues<Permission>())
            {
                var roles = permission is Permission.CatalogueManage or Permission.InquiryManage or Permission.SettingsManage
                    ? new[] { nameof(RoleName.SalesAdmin), nameof(RoleName.MasterAdmin) }
                    : new[] { nameof(RoleName.MasterAdmin) };
                options.AddPolicy(permission.ToString(), policy => policy.RequireAuthenticatedUser().RequireRole(roles));
            }
        });
        return services;
    }
}
