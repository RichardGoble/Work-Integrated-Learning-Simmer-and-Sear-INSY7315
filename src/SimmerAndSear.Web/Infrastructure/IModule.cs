using System.Reflection;

namespace SimmerAndSear.Web.Infrastructure;
public interface IModule
{
    void Register(IServiceCollection services, IConfiguration configuration);
}

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
