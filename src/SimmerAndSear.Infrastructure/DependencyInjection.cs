using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Infrastructure.Caching;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Infrastructure.Repositories;
using SimmerAndSear.Infrastructure.Security;
using SimmerAndSear.Infrastructure.Storage;

namespace SimmerAndSear.Infrastructure;
// ── References ──────────────────────────────────────────────────────────────
//Microsoft, 2026. Dependency injection in ASP.NET Core [Source code].
//Available at: <https://learn.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection?view=aspnetcore-10.0> [Accessed 18 September 2026].
//
//Microsoft, 2026. Options pattern [Source code].
//Available at: <https://learn.microsoft.com/en-us/dotnet/core/extensions/options> [Accessed 18 September 2026].
// ────────────────────────────────────────────────────────────────────────────
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        MongoConventions.Register();

        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "Mongo:ConnectionString is not configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Database),
                "Mongo:Database is not configured.")
            .ValidateOnStart();
        services.AddSingleton<IMongoClient>(sp =>
            new MongoClient(sp.GetRequiredService<IOptions<MongoOptions>>().Value.ConnectionString));

        services.AddSingleton(sp => sp.GetRequiredService<IMongoClient>()
            .GetDatabase(sp.GetRequiredService<IOptions<MongoOptions>>().Value.Database));

        services.AddSingleton<IGridFSBucket>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MongoOptions>>().Value;

            return new GridFSBucket(
                sp.GetRequiredService<IMongoClient>().GetDatabase(options.Database),
                new GridFSBucketOptions { BucketName = options.FileBucket });
        });

        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IAuditLogRepository, AuditLogRepository>();
        services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();
        services.AddScoped<IMediaAssetRepository, MediaAssetRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<INewsPostRepository, NewsPostRepository>();
        services.AddMemoryCache();
        services.AddScoped<ISiteSettingsProvider, CachedSiteSettingsProvider>();

        services.AddScoped<IFileStore, GridFsFileStore>();
        services.AddScoped<IDatabaseHealth, MongoDatabaseHealth>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

     
        services.AddSingleton<SchemaStatus>();
        services.AddSingleton<ISchemaStatus>(sp => sp.GetRequiredService<SchemaStatus>());

        services.AddHostedService<MongoSchemaInitializer>();

        return services;
    }
}
