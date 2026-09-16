using System.Reflection;

namespace SimmerAndSear.Web.Infrastructure;

/// <summary>
/// A feature's own service registration.
/// <para>
/// Five people editing Program.cs to register their dependencies is a guaranteed
/// daily merge conflict. Each feature implements this instead, is discovered by
/// assembly scanning, and Program.cs never changes again.
/// </para>
/// </summary>
public interface IModule
{
    void Register(IServiceCollection services, IConfiguration configuration);
}

public static class ModuleExtensions
{
    /// <summary>
    /// Finds every <see cref="IModule"/> in the assembly and lets each register
    /// its own services. Called exactly once, from Program.cs.
    /// </summary>
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
