using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SimmerAndSear.Application.Abstractions;
using SimmerAndSear.Application.Engagement;
using SimmerAndSear.Infrastructure.Engagement;
using SimmerAndSear.Infrastructure.Repositories;
using SimmerAndSear.Web.Infrastructure;

namespace SimmerAndSear.Web.Modules.Engagement;

public sealed class EngagementModule : IModule
{
    public void Register(IServiceCollection services, IConfiguration configuration)
    {
        // Options
        services.Configure<BrevoOptions>(configuration.GetSection(BrevoOptions.SectionName));

        // Application Services
        services.AddScoped<ContactService>();
        services.AddScoped<BookingService>();

        // Repositories
        services.AddScoped<IBookingRepository, BookingRepository>();

        // Queue & Transactional Email
        services.AddSingleton<IContactNotificationQueue, ContactNotificationQueue>();
        services.AddHttpClient<ITransactionalEmailSender, BrevoTransactionalEmailSender>(client =>
        {
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        // Background Worker
        services.AddHostedService<ContactNotificationWorker>();
    }
}
