using Microsoft.AspNetCore.HttpOverrides;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Infrastructure;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Web.Infrastructure;
using SimmerAndSear.Web.Security;
MongoConventions.Register();

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Deployment:IsHostedEnvironment"))
{
    throw new InvalidOperationException(
        "Development environment detected on a hosted deployment. Refusing to start: " +
        "the developer exception page would expose stack traces publicly.");
}

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();
builder.Services.AddAccountIdentity();
builder.Services.AddAccountAuthorization();
builder.Services.AddSecurityHardening();
builder.WebHost.ConfigureKestrel(options =>
{
    options.AddServerHeader = false;
    options.Limits.MaxRequestBodySize = 10 * 1024 * 1024;
});
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddModulesFrom(typeof(Program).Assembly, builder.Configuration);
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;


    foreach (var proxy in builder.Configuration.GetSection("Security:KnownProxies").Get<string[]>() ?? [])
        o.KnownProxies.Add(System.Net.IPAddress.Parse(proxy));
});

var app = builder.Build();

app.UseForwardedHeaders();

app.UseSecurityHeaders();
app.UseExceptionHandler();
if (!app.Environment.IsDevelopment()) app.UseHsts();
app.UseStatusCodePages(async context =>
    await Results.Problem(statusCode: context.HttpContext.Response.StatusCode).ExecuteAsync(context.HttpContext));

app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
   .WithName("HealthLive");
app.MapGet("/health/ready", async (IDatabaseHealth health, CancellationToken ct) =>
{
    var reachable = await health.IsReachableAsync(ct);
    return reachable
        ? Results.Ok(new { status = "ready", database = "up" })
        : Results.Json(new { status = "degraded", database = "down" }, statusCode: 503);
}).WithName("HealthReady");

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Use cookie authentication without ASP.NET Core Identity [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/security/authentication/cookie?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Health checks in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public partial class Program;
