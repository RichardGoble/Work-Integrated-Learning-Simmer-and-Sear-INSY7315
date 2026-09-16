using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimmerAndSear.Infrastructure;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Seeder;

// FIRST, exactly as in the web application. The driver caches a class map on
// first use, and conventions registered afterwards are silently ignored.
MongoConventions.Register();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddScoped<SeedRunner>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();

// Starting the host runs MongoSchemaInitializer, so the seeder creates the
// collections, validators and indexes before it writes the first document.
await host.StartAsync();

int problems;

try
{
    using var scope = host.Services.CreateScope();
    problems = await scope.ServiceProvider.GetRequiredService<SeedRunner>().RunAsync();
}
catch (Exception ex)
{
    logger.LogError(ex, "Seeding failed.");
    problems = 1;
}

await host.StopAsync();

if (problems == 0)
{
    logger.LogInformation("Seeding complete.");
}
else
{
    logger.LogWarning("Seeding finished with {Count} problem(s). See the messages above.", problems);
}

// Non-zero on any problem, so a pipeline step or a script can tell the
// difference between "seeded" and "ran without doing very much".
return problems == 0 ? 0 : 1;

/// <summary>Marker for the logger category.</summary>
public partial class Program;
