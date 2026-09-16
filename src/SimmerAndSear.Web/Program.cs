using Microsoft.AspNetCore.HttpOverrides;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Infrastructure;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Web.Infrastructure;

// FIRST. The MongoDB driver builds and caches a class map the moment it first
// serialises a type, and conventions registered after that point are silently
// ignored. Nothing above this line may touch a model.
MongoConventions.Register();

var builder = WebApplication.CreateBuilder(args);

// An unset ASPNETCORE_ENVIRONMENT defaults to Production on App Service, but a
// developer setting it to Development on a deployed app would publish stack
// traces. Fail loudly at start-up rather than leak quietly.
if (builder.Environment.IsDevelopment() &&
    builder.Configuration.GetValue<bool>("Deployment:IsHostedEnvironment"))
{
    throw new InvalidOperationException(
        "Development environment detected on a hosted deployment. Refusing to start: " +
        "the developer exception page would expose stack traces publicly.");
}

builder.Services.AddControllersWithViews();
builder.Services.AddProblemDetails();

// The only place the web layer names the infrastructure layer.
builder.Services.AddInfrastructure(builder.Configuration);

// Every feature registers itself. Do not add service registrations below this line.
builder.Services.AddModulesFrom(typeof(Program).Assembly, builder.Configuration);

// App Service terminates TLS at its front end. Without this the application sees
// the proxy's address for every request, which breaks rate limiting (all callers
// share one partition) and makes generated URLs come out as http://.
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;


    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

var app = builder.Build();

app.UseForwardedHeaders();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthorization();

app.MapStaticAssets();

// --- health ------------------------------------------------------------------

// Liveness: the process is running. Deliberately touches nothing else, so a
// database outage never causes the platform to recycle an otherwise healthy app.
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }))
   .WithName("HealthLive");

// Readiness: the whole stack is wired. This is what the pipeline's smoke gate
// calls - a deploy that cannot tell you whether the application came up is not
// a pipeline.
app.MapGet("/health/ready", async (IDatabaseHealth health, CancellationToken ct) =>
{
    var reachable = await health.IsReachableAsync(ct);

    // The failure detail is logged, never returned: a public probe must not
    // describe the internals of what is broken.
    return reachable
        ? Results.Ok(new { status = "ready", database = "up" })
        : Results.Json(new { status = "degraded", database = "down" }, statusCode: 503);
}).WithName("HealthReady");

// -----------------------------------------------------------------------------

app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();

/// <summary>
/// Exposed so WebApplicationFactory&lt;Program&gt; can find the entry point of a
/// top-level-statements program. Without it the integration tests do not compile.
/// </summary>
public partial class Program;
