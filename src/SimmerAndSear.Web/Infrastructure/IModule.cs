using System.Reflection;

namespace SimmerAndSear.Web.Infrastructure;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
//
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public interface IModule
{
    void Register(IServiceCollection services, IConfiguration configuration);
}

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public static class ModuleExtensions
{
    public static IServiceCollection AddModulesFrom(
        this IServiceCollection services, Assembly assembly, IConfiguration configuration)
    {
        var moduleTypes = assembly.GetTypes()
            .Where(t => typeof(IModule).IsAssignableFrom(t)
                        && t is { IsAbstract: false, IsInterface: false });

        foreach (var type in moduleTypes)
        {
            var module = (IModule)Activator.CreateInstance(type)!;
            module.Register(services, configuration);
        }

        return services;
    }
}
