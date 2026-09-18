using System.Reflection;
using NetArchTest.Rules;
using Shouldly;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Domain.Entities;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Web.Infrastructure;

namespace SimmerAndSear.ArchitectureTests;
public sealed class LayeringTests
{
    private static readonly Assembly Domain = typeof(Product).Assembly;
    private static readonly Assembly Application = typeof(IProductRepository).Assembly;
    private static readonly Assembly Infrastructure = typeof(MongoConventions).Assembly;
    private static readonly Assembly Web = typeof(IModule).Assembly;

    private const string MongoNamespace = "MongoDB";
    private const string InfrastructureNamespace = "SimmerAndSear.Infrastructure";

    [Fact]
    public void Domain_depends_on_nothing_of_ours_and_no_storage_technology()
    {
        var result = Types.InAssembly(Domain)
            .ShouldNot()
            .HaveDependencyOnAny(
                MongoNamespace,
                InfrastructureNamespace,
                "SimmerAndSear.Application",
                "SimmerAndSear.Contracts",
                "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Application_depends_on_abstractions_not_on_the_database()
    {
        var result = Types.InAssembly(Application)
            .ShouldNot()
            .HaveDependencyOnAny(MongoNamespace, InfrastructureNamespace, "Microsoft.AspNetCore")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Controllers_never_reach_past_the_application_layer()
    {
        var result = Types.InAssembly(Web)
            .That()
            .ResideInNamespaceStartingWith("SimmerAndSear.Web.Controllers")
            .ShouldNot()
            .HaveDependencyOnAny(MongoNamespace, InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Api_endpoints_never_reach_past_the_application_layer()
    {
        var apiTypes = Types.InAssembly(Web)
            .That()
            .ResideInNamespaceStartingWith("SimmerAndSear.Web.Api")
            .GetTypes();
        if (apiTypes.Any() is false)
        {
            return;
        }

        var result = Types.InAssembly(Web)
            .That()
            .ResideInNamespaceStartingWith("SimmerAndSear.Web.Api")
            .ShouldNot()
            .HaveDependencyOnAny(MongoNamespace, InfrastructureNamespace)
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    [Fact]
    public void Repositories_live_only_in_the_infrastructure_layer()
    {
        var result = Types.InAssembly(Infrastructure)
            .That()
            .HaveNameEndingWith("Repository")
            .Should()
            .ResideInNamespaceStartingWith("SimmerAndSear.Infrastructure")
            .GetResult();

        result.IsSuccessful.ShouldBeTrue(Describe(result));
    }

    private static string Describe(TestResult result) =>
        result.FailingTypeNames is { Count: > 0 } failing
            ? $"Layering violated by: {string.Join(", ", failing)}"
            : "Layering violated.";
}
