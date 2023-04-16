namespace Outbox.Publisher.RabbitMQ.Tests;

using global::RabbitMQ.Client.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.Core;
using Outbox.Publisher.RabbitMQ;
using System;
using System.Net.NetworkInformation;
using TimeoutException = Outbox.Publisher.RabbitMQ.TimeoutException;

[Collection("Sequential")]
public class OutboxPublisherRabbitMQTests : TestBase, IDisposable
{
    public const int MaxMessageSize = 1024; // see ./.docker/rabbitmq/etc/rabbitmq.conf
    public const string RabbitToxicPort = "17000"; // see docker-compose.yml, adds 500ms latency to any data
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
        const int NonListeningPort = 44000;
        bool portListening = IPGlobalProperties.GetIPGlobalProperties().GetActiveTcpListeners().Any(x => x.Port == NonListeningPort);
        Assert.False(portListening, $"Port {NonListeningPort} is used on your machine. Change the constant.");

        Dictionary<string, string?> configurationOverrides = new()
        {
            { $"{OutboxPublisherOptions.DefaultSectionName}:Port", NonListeningPort.ToString() }
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
        this.Setup();
        OutboxMessage message = GenerateRndMessage(topic: "invalid.topic");

        // act
        DeliveryException ex = await Assert.ThrowsAsync<DeliveryException>(() => _publisher!.PublishAsync(message));

        // verify
        Assert.Equal("NO_ROUTE", ex.Message);
        Assert.Equal(message.Topic, ex.RoutingKey);
        Assert.Equal(312, ex.ReplyCode); // RabbitMQ.Client.Constants.NoConsumers is 313
    }

    [Fact]
    public async Task PublishAsync_Throws_TimeoutException_When_Exceeding_ConnectTimeout()
    {
        // setup
        Dictionary<string, string?> configurationOverrides = new()
        {
            { $"{OutboxPublisherOptions.DefaultSectionName}:PublishTimeout", "00:10:00" }
        };
        this.Setup(configurationOverrides);
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
        Dictionary<string, string?> configurationOverrides = new()
        {
            { $"{OutboxPublisherOptions.DefaultSectionName}:Port", RabbitToxicPort }
        };
        this.Setup(configurationOverrides);
        OutboxMessage message = GenerateRndMessage();

        ////_publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>();

        ////ConnectionFactory connectionFactory = (ConnectionFactory)_host.Services.GetService<IConnectionFactory>()!;
        ////connectionFactory.Port = RabbitToxicPort; 
        ////_publisher = (OutboxPublisher)_host.Services.GetRequiredService<IOutboxPublisher>(); // re-resolve so the new timeout is applied
        //////_publisher.Options.PublishTimeout = TimeSpan.FromMilliseconds(99);

        // act
        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(() => _publisher!.PublishAsync(message));

        // verify
        Assert.Equal(TimeoutException.CannotConnectMessage, ex.Message);
        Assert.IsType<BrokerUnreachableException>(ex.InnerException);
    }

    [Fact]
    public async Task PublishAsync_Sends_A_Message_To_Specified_Topic()
    {
        // setup
        this.Setup();
        OutboxMessage message = GenerateRndMessage(); // rnd topic generated
        await _publisher!.PurgeAsync(message.Topic);

        // act
        await _publisher!.PublishAsync(message);

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
