using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SimmerAndSear.IntegrationTests;
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
