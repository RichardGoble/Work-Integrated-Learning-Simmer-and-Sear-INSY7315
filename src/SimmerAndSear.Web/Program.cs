using Microsoft.AspNetCore.HttpOverrides;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Infrastructure;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Web.Infrastructure;
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
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddModulesFrom(typeof(Program).Assembly, builder.Configuration);
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
public partial class Program;
