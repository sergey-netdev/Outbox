namespace Outbox.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;

public static class ConfigurationExtensions
{
    public static void AddOutboxService(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        OutboxServiceOptions options = new();
        configuration.GetSection(OutboxServiceOptions.DefaultSectionName).Bind(options);
        services.AddSingleton(options);

        services.AddSingleton<IOutboxService, OutboxService>();
    }
}
