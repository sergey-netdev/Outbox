namespace Outbox.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;

public static class ConfigurationExtensions
{
    public static void AddOutboxService(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddSingleton(serviceProvider =>
        {
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
            OutboxServiceOptions options = new();
            configuration.GetSection(OutboxServiceOptions.DefaultSectionName).Bind(options);
            return options;
        });

        services.AddTransient<IOutboxService, OutboxService>();
    }
}
