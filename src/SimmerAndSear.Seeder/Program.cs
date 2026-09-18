using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SimmerAndSear.Infrastructure;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Seeder;
MongoConventions.Register();

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddScoped<SeedRunner>();

using var host = builder.Build();

var logger = host.Services.GetRequiredService<ILogger<Program>>();
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
return problems == 0 ? 0 : 1;
public partial class Program;
