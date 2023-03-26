namespace Outbox.Publisher.RabbitMQ;

using global::RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;

public static class ConfigurationExtensions
{
    public static void AddOutboxRabbitMQPublisher(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));
        ArgumentNullException.ThrowIfNull(configuration, nameof(configuration));

        PublisherOptions options = new();
        configuration.GetSection(PublisherOptions.DefaultSectionName).Bind(options);
        services.AddSingleton(options);

        ConnectionFactory factory = new()
        {
            HostName = options.HostName,
            Port = options.Port,
            UserName = options.UserName,
            Password = options.Password,
        };
        services.AddSingleton<IConnectionFactory>(factory);
        services.AddSingleton<IPublisher, Publisher>();
    }
}
