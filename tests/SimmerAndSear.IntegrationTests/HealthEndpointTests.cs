using System.Net;
using Shouldly;

namespace SimmerAndSear.IntegrationTests;

/// <summary>
/// The walking skeleton, asserted. These two endpoints are what the deployment
/// pipeline's smoke gate calls, so if they regress the pipeline stops telling the
/// truth about whether a release came up.
/// </summary>
public sealed class HealthEndpointTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Liveness_reports_the_process_is_running()
    {
        var response = await factory.CreateClient().GetAsync("/health/live");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("live");
    }

    [Fact]
    public async Task Readiness_reports_the_database_is_reachable()
    {
        var response = await factory.CreateClient().GetAsync("/health/ready");

        // A 503 here means Mongo is not running. Start one with:
        //   docker run -d -p 27017:27017 --name simmersear-mongo mongo:7
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("up");
    }
}
