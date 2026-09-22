using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimmerAndSear.Application.Quotations;
using SimmerAndSear.Web.Infrastructure;

namespace SimmerAndSear.Web.Modules.Quotations;

//References
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 21 September 2026].

//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 21 September 2026].

public sealed class QuotationsModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<QuotationService>();
    }
}
