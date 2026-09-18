using System.Net;
using Shouldly;

namespace SimmerAndSear.IntegrationTests;
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
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).ShouldContain("up");
    }
}
