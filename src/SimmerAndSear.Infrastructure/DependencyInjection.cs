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
        services.AddScoped<IFileStore, GridFsFileStore>();
        services.AddScoped<IDatabaseHealth, MongoDatabaseHealth>();
        services.AddSingleton<IPasswordHasher, PasswordHasher>();
        services.AddHostedService<MongoSchemaInitializer>();

        return services;
    }
}
