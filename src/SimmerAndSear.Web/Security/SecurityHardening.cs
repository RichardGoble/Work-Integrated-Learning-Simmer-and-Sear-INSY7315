using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Core.Infrastructure;
using SimmerAndSear.Domain.Enums;

namespace SimmerAndSear.Web.Security;

public static class SecurityHardening
{
    public const string AccountPolicy = "accounts";

    public static IServiceCollection AddSecurityHardening(this IServiceCollection services)
    {
        services.AddExceptionHandler<SafeExceptionHandler>();
        services.AddHsts(options => { options.MaxAge = TimeSpan.FromDays(365); options.IncludeSubDomains = true; });
        services.Configure<FormOptions>(options =>
        {
            options.ValueLengthLimit = 16 * 1024;
            options.ValueCountLimit = 100;
            options.MultipartBodyLengthLimit = 10 * 1024 * 1024;
        });
        services.Configure<MvcOptions>(options => options.Filters.Add<AntiforgeryFailureFilter>(-3000));
        services.Configure<ApiBehaviorOptions>(options => options.InvalidModelStateResponseFactory = context =>
            new BadRequestObjectResult(new ValidationProblemDetails(context.ModelState.Keys.ToDictionary(
                key => key, _ => new[] { "The supplied value is invalid." }))
            { Status = 400, Title = "Check the supplied values." }));
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions { PermitLimit = 120, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.AddPolicy(AccountPolicy, context => RateLimitPartition.GetFixedWindowLimiter(
                context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                _ => new FixedWindowRateLimiterOptions { PermitLimit = 10, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
            options.OnRejected = async (context, ct) =>
            {
                context.HttpContext.Response.Headers.RetryAfter = "60";
                await context.HttpContext.RequestServices.GetRequiredService<AuditWriter>()
                    .WriteAsync(AuditAction.AccessDenied, "ACCESS_DENIED rate limit exceeded");
                await Results.Problem(statusCode: 429, title: "Too many requests. Try again later.").ExecuteAsync(context.HttpContext);
            };
        });
        return services;
    }

    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app) => app.Use(async (context, next) =>
    {
        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;
            headers.XContentTypeOptions = "nosniff";
            headers.XFrameOptions = "DENY";
            headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
            headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";
            headers.ContentSecurityPolicy = "default-src 'self'; script-src 'self'; script-src-attr 'none'; style-src 'self' 'unsafe-inline'; img-src 'self' data:; font-src 'self'; connect-src 'self'; object-src 'none'; base-uri 'self'; form-action 'self'; frame-ancestors 'none'";
            if (context.Request.Path.StartsWithSegments("/api") || context.User.Identity?.IsAuthenticated == true)
                headers.CacheControl = "no-store";
            return Task.CompletedTask;
        });
        await next();
    });
}

public sealed class SafeExceptionHandler(ILogger<SafeExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception exception, CancellationToken ct)
    {
        logger.LogError(exception, "Request failed. Trace {TraceId}", context.TraceIdentifier);
        var status = exception is BadHttpRequestException badRequest ? badRequest.StatusCode : 500;
        await Results.Problem(statusCode: status, title: status == 500 ? "Something went wrong." : "Invalid request.",
            extensions: new Dictionary<string, object?> { ["traceId"] = context.TraceIdentifier }).ExecuteAsync(context);
        return true;
    }
}

public sealed class AntiforgeryFailureFilter(AuditWriter audit) : IAsyncAlwaysRunResultFilter, IOrderedFilter
{
    public int Order => -3000;

    public async Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
    {
        if (context.Result is IAntiforgeryValidationFailedResult)
        {
            await audit.WriteAsync(AuditAction.AccessDenied, "ACCESS_DENIED invalid antiforgery token");
            context.Result = new BadRequestObjectResult(new ProblemDetails
            { Status = 400, Title = "Invalid security token. Refresh the page and try again." });
        }
        await next();
    }
}
