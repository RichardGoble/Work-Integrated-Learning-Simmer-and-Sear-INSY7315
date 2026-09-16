using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace SimmerAndSear.IntegrationTests;

/// <summary>
/// Boots the real application against a real MongoDB.
///
/// <para>
/// Integration tests run against an actual database - a container locally
/// (<c>docker run -d -p 27017:27017 mongo:7</c>) and a service container in CI -
/// so queries, indexes, GridFS and $jsonSchema rejections all behave exactly as
/// they will in production. Nothing here needs a secret, which is what lets the
/// pipeline run these on every pull request.
/// </para>
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    private const string DefaultConnectionString = "mongodb://localhost:27017";
    private const string DefaultDatabase = "simmersear_it";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Not Development: that would enable the developer exception page and
        // mask the error responses these tests are meant to assert on.
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
