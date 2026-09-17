using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Infrastructure.Persistence;
using SimmerAndSear.Infrastructure.Repositories;
using SimmerAndSear.Infrastructure.Security;
using SimmerAndSear.Infrastructure.Storage;

namespace SimmerAndSear.Infrastructure;

/// <summary>
/// The single seam between the web layer and the infrastructure layer. Program.cs
/// calls <see cref="AddInfrastructure"/> and never names a MongoDB type itself.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services, IConfiguration configuration)
    {
        // Conventions before anything else can trigger a class map. Program.cs
        // calls this first too; the registration is idempotent.
        MongoConventions.Register();

        services.AddOptions<MongoOptions>()
            .Bind(configuration.GetSection(MongoOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString),
                "Mongo:ConnectionString is not configured.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Database),
                "Mongo:Database is not configured.")
            .ValidateOnStart();

    

        // SINGLETON, deliberately. The driver pools connections internally; a
        // per-request MongoClient exhausts sockets under Basic-tier App Service
        // limits and is the most common way to break a .NET Mongo application.
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
        services.AddScoped<IFileStore, GridFsFileStore>();
        services.AddScoped<IDatabaseHealth, MongoDatabaseHealth>();

        // Stateless and thread-safe.
        services.AddSingleton<IPasswordHasher, PasswordHasher>();

        // Indexes and $jsonSchema validators, applied idempotently on every boot.
        services.AddHostedService<MongoSchemaInitializer>();

        return services;
    }
}
