namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.Core;
using Outbox.Publisher.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Security.AccessControl;
using TimeoutException = Outbox.Publisher.RabbitMQ.TimeoutException;

public class OutboxPublisherRabbitMQTests : TestBase, IDisposable
{
    public const int MaxMessageSize = 1024; // see ./.docker/rabbitmq/etc/rabbitmq.conf
    public const int RabbitToxicPort = 17000; // see docker-compose.yml
    private OutboxPublisher? _publisher;
    private IHost? _host;
    private readonly CancellationTokenSource _readCts = new(TimeSpan.FromMilliseconds(300));

    private new void Setup(Dictionary<string, string?>? configurationOverrides = null)
    {
        IHostBuilder hostBuilder = base.Setup(configurationOverrides);
        _host = hostBuilder.ConfigureServices(services => { services.AddOutboxRabbitMQPublisher(); }).Build();
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>();
    }

    public void Dispose()
    {
        _host?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PublishAsync_Throws_TimeoutException_When_Broker_Is_Unavailable()
    {
        // setup
        Dictionary<string, string?> configurationOverrides = new()
        {
            { $"{OutboxPublisherOptions.DefaultSectionName}:Port", "17001" /* see toxiproxy config */ }
        };
        this.Setup(configurationOverrides);
        OutboxMessage message = GenerateRndMessage();

        // act
        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(() => _publisher!.PublishAsync(message));

        // verify
        Assert.Equal(TimeoutException.CannotConnectMessage, ex.Message);
    }

    [Fact]
    public async Task PublishAsync_Throws_DeliveryException_When_Topic_Is_Invalid()
    {
        // setup
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>();
        OutboxMessage message = GenerateRndMessage(topic: "invalid.topic");

        // act
        DeliveryException ex = await Assert.ThrowsAsync<DeliveryException>(() => _publisher.PublishAsync(message));

        // verify
        Assert.Equal("NO_ROUTE", ex.Message);
        Assert.Equal(message.Topic, ex.RoutingKey);
        Assert.Equal(312, ex.ReplyCode); // RabbitMQ.Client.Constants.NoConsumers is 313
    }

    [Fact]
    public async Task PublishAsync_Throws_TimeoutException_When_Exceeding_ConnectTimeout()
    {
        // setup
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>();

        ConnectionFactory connectionFactory = (ConnectionFactory)_host.Services.GetService<IConnectionFactory>()!;
        connectionFactory.RequestedConnectionTimeout = TimeSpan.FromMicroseconds(1);
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>(); // re-resolve so the new timeout is applied
        _publisher.Options.PublishTimeout = connectionFactory.RequestedConnectionTimeout * 10;

        OutboxMessage message = GenerateRndMessage();

        // act
        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(() => _publisher.PublishAsync(message));

        // verify
        Assert.Equal(TimeoutException.CannotConnectMessage, ex.Message);
        Assert.IsType<BrokerUnreachableException>(ex.InnerException);
    }

    [Fact]
    public async Task PublishAsync_Throws_TimeoutException_When_Exceeding_PublishTimeout()
    {
        // setup
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>();

        ConnectionFactory connectionFactory = (ConnectionFactory)_host.Services.GetService<IConnectionFactory>()!;
        connectionFactory.Port = RabbitToxicPort; // adds 500ms latency to any data
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>(); // re-resolve so the new timeout is applied
        //_publisher.Options.PublishTimeout = TimeSpan.FromMilliseconds(99);

        OutboxMessage message = GenerateRndMessage();

        // act
        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(() => _publisher.PublishAsync(message));

        // verify
        Assert.Equal(TimeoutException.CannotConnectMessage, ex.Message);
        Assert.IsType<BrokerUnreachableException>(ex.InnerException);
    }

    [Fact]
    public async Task PublishAsync_Sends_A_Message_To_Specified_Topic()
    {
        // setup
        _publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>();
        OutboxMessage message = GenerateRndMessage(); // rnd topic generated

        // act
        await _publisher.PublishAsync(message);

        // verify
        IOutboxMessageBase? receivedMessage = await PullMessageAsync(message.Topic);
        Assert.NotNull(receivedMessage);
        Assert.Equal(message.Payload, receivedMessage.Payload);
    }

    private async Task<IOutboxMessageBase?> PullMessageAsync(string topic)
    {
        if (_publisher is null)
        {
            throw new InvalidOperationException();
        };

        byte[]? payload = await _publisher.ReadMessageAsync(topic);
        return payload != null ? new OutboxMessageBase(payload) : null;
    }

    private async Task<IReadOnlyCollection<IOutboxMessageBase>> PullMessagesAsync(string topic)
    {
        if (_publisher is null)
        {
            throw new InvalidOperationException();
        };

        IReadOnlyCollection<byte[]> payloads = await _publisher.ReadBatchAsync(topic, 10, TimeSpan.FromMilliseconds(100), CancellationToken.None);
        OutboxMessageBase[] result = payloads.Select(x => new OutboxMessageBase(x)).ToArray();
        return result;
    }
}
