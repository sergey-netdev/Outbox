namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Outbox.Core;
using Outbox.Publisher.RabbitMQ;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using TimeoutException = Outbox.Publisher.RabbitMQ.TimeoutException;

public class OutboxPublisherRabbitMQTests : TestBase, IDisposable
{
    public const int MaxMessageSize = 1024; // see ./.docker/rabbitmq/etc/rabbitmq.conf
    private readonly IHost _host;
    private Publisher _publisher;
    private readonly CancellationTokenSource _readCts = new(TimeSpan.FromMilliseconds(300));
    ////private readonly Dictionary<string, string> _configurationOverrides = new()
    ////    {
    ////    };

    public OutboxPublisherRabbitMQTests()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            ////.AddInMemoryCollection(_configurationOverrides!)
            .Build();

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddOutboxRabbitMQPublisher(configuration);
            });

        _host = hostBuilder.Build();
        _publisher = (Publisher)_host.Services.GetRequiredService<IPublisher>();
    }

    public void Dispose()
    {
        _host?.Dispose();
        GC.SuppressFinalize(this);
    }

    [Fact]
    public async Task PublishAsync_Throws_DeliveryException_When_Topic_Is_Invalid()
    {
        // setup
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
        ConnectionFactory connectionFactory = (ConnectionFactory)_host.Services.GetService<IConnectionFactory>()!;
        connectionFactory.RequestedConnectionTimeout = TimeSpan.FromMicroseconds(1);
        _publisher = (Publisher)_host.Services.GetRequiredService<IPublisher>(); // re-resolve so the new timeout is applied
        _publisher.Options.PublishTimeout = connectionFactory.RequestedConnectionTimeout * 10;

        OutboxMessage message = GenerateRndMessage();

        // act
        TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(() => _publisher.PublishAsync(message));

        // verify
        Assert.Equal(TimeoutException.CannotConnectMessage, ex.Message);
        Assert.IsType<BrokerUnreachableException>(ex.InnerException);
    }

    ////[Fact]
    ////public async Task PublishAsync_Throws_TimeoutException_When_Exceeding_PublishTimeout()
    ////{
    ////    // setup
    ////    _publisher.Options.PublishTimeout = TimeSpan.FromMicroseconds(0);

    ////    OutboxMessage message = GenerateRndMessage();

    ////    // act
    ////    TimeoutException ex = await Assert.ThrowsAsync<TimeoutException>(() => _publisher.PublishAsync(message));

    ////    // verify
    ////    Assert.Equal(TimeoutException.CannotConnectMessage, ex.Message);
    ////    Assert.IsType<BrokerUnreachableException>(ex.InnerException);
    ////}

    [Fact]
    public async Task PublishAsync_Sends_A_Message_To_Specified_Topic()
    {
        // setup
        OutboxMessage message = GenerateRndMessage(); // rnd topic generated

        // act
        await _publisher.PublishAsync(message);

        // verify
        IReadOnlyCollection<IOutboxMessageBase> messages = await PullMessagesAsync(message.Topic);
        IOutboxMessageBase receivedMessage = Assert.Single(messages);
        Assert.Equal(message.Payload, receivedMessage.Payload);
    }

    private async Task<IReadOnlyCollection<IOutboxMessageBase>> PullMessagesAsync(string topic)
    {
        IReadOnlyCollection<ReadOnlyMemory<byte>> payloads = await _publisher.ReadAllAsync(topic, _readCts.Token);
        OutboxMessageBase[] result = payloads.Select(x => new OutboxMessageBase(x.ToArray())).ToArray();
        return result;
    }
}
