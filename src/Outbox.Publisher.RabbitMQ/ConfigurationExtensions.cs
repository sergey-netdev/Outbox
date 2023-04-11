namespace Outbox.Publisher.RabbitMQ;

using global::RabbitMQ.Client;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Outbox.Core;

public static class ConfigurationExtensions
{
    public static void AddOutboxRabbitMQPublisher(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services, nameof(services));

        services.AddSingleton(serviceProvider =>
        {
            IConfiguration configuration = serviceProvider.GetRequiredService<IConfiguration>();
            OutboxPublisherOptions options = new();
            configuration.GetSection(OutboxPublisherOptions.DefaultSectionName).Bind(options);
            return options;
        });

        services.AddSingleton<IConnectionFactory>(serviceProvider =>
        {
            OutboxPublisherOptions options = serviceProvider.GetRequiredService<OutboxPublisherOptions>();
            ConnectionFactory connectionFactory = new()
            {
                HostName = options.HostName,
                Port = options.Port,
                UserName = options.UserName,
                Password = options.Password,
                ContinuationTimeout = options.ContinuationTimeout,
                HandshakeContinuationTimeout = options.HandshakeContinuationTimeout,
                RequestedConnectionTimeout = options.RequestedConnectionTimeout,
                RequestedHeartbeat = options.RequestedHeartbeat,
                SocketReadTimeout = options.SocketReadTimeout,
                SocketWriteTimeout = options.SocketWriteTimeout,
            };
            return connectionFactory;
        });
        services.AddSingleton<IOutboxPublisher, OutboxPublisher>();
    }
}
