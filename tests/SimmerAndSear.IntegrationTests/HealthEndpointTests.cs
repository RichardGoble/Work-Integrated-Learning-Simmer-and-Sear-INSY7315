using System.Net;
using Shouldly;

namespace SimmerAndSear.IntegrationTests;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Integration tests in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Health checks in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
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

    [Fact]
    public async Task Readiness_reports_the_state_of_the_database_schema()
    {
        var response = await factory.CreateClient().GetAsync("/health/ready");
        var body = await response.Content.ReadAsStringAsync();

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        body.ShouldContain("\"schema\":\"applied\"");

        body.ShouldContain("\"detail\":null");
    }
}
