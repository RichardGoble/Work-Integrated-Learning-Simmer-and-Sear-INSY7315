using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Net.Http.Headers;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Media;
using SimmerAndSear.Infrastructure.Media;
using SimmerAndSear.Web.Infrastructure;

namespace SimmerAndSear.Web.Modules.Media;

// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 20 September 2026].
//
//Microsoft, 2026. Interfaces - define behavior for multiple types [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/csharp/fundamentals/types/interfaces> [Accessed 20 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class MediaModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<IImageProcessor, ImageSharpImageProcessor>();
        services.AddScoped<IMediaService, MediaService>();
        services.AddSingleton<IStartupFilter, MediaCachingStartupFilter>();
    }
}

internal sealed class MediaCachingStartupFilter : IStartupFilter
{
    private const string ImmutableCacheControl = "public, max-age=31536000, immutable";

    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use(async (context, nextMiddleware) =>
            {
                if (context.Request.Path.StartsWithSegments("/api/media") &&
                    HttpMethods.IsGet(context.Request.Method))
                {
                    context.Response.OnStarting(() =>
                    {
                        if (context.Response.StatusCode is StatusCodes.Status200OK or StatusCodes.Status304NotModified)
                        {
                            context.Response.Headers[HeaderNames.CacheControl] = ImmutableCacheControl;
                        }

                        return Task.CompletedTask;
                    });
                }

                await nextMiddleware();
            });

            next(app);
        };
    }
}

