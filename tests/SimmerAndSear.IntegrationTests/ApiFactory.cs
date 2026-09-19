using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SimmerAndSear.IntegrationTests;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//xUnit.net, 2025. Getting Started with xUnit.net v2 [Source code].
//Available at: <https://xunit.net/docs/getting-started/v2/getting-started> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString = "mongodb://localhost:27017";
    private const string DefaultDatabase = "simmersear_it";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Mongo:ConnectionString"] =
                    Environment.GetEnvironmentVariable("Mongo__ConnectionString") ?? DefaultConnectionString,

                ["Mongo:Database"] =
                    Environment.GetEnvironmentVariable("Mongo__Database") ?? DefaultDatabase,

                ["Mongo:ApplySchemaOnStartup"] = "true"
            });
        });
    }
}
