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

        OutboxPublisherOptions options = new();
        configuration.GetSection(OutboxPublisherOptions.DefaultSectionName).Bind(options);
        services.AddSingleton(options);

        ConnectionFactory factory = new()
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
        services.AddSingleton<IConnectionFactory>(factory);
        services.AddSingleton<IOutboxPublisher, OutboxPublisher>();
    }
}
