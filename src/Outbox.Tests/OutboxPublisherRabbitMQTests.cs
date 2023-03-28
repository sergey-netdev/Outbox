namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.Core;
using Outbox.Publisher.RabbitMQ;
using System;

public class OutboxPublisherRabbitMQTests : TestBase, IDisposable
{
    private readonly IHost _host;
    private readonly Publisher _publisher;
    private readonly CancellationTokenSource _readCts = new(TimeSpan.FromMilliseconds(300));

    public OutboxPublisherRabbitMQTests()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
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
    }

    [Fact]
    public async Task PublishAsync_Sends_A_Message()
    {
        // setup
        OutboxMessage message = GenerateRndMessage();

        // act
        await _publisher.PublishAsync(message);

        // verify
        IReadOnlyCollection<IOutboxMessageBase> messages = await PullMessagesAsync();
        IOutboxMessageBase receivedMessage = Assert.Single(messages);
        Assert.Equal(message.Payload, receivedMessage.Payload);
    }

    private async Task<IReadOnlyCollection<IOutboxMessageBase>> PullMessagesAsync()
    {
        IReadOnlyCollection<ReadOnlyMemory<byte>> payloads = await _publisher.ReadAllAsync(DefaultTopic, _readCts.Token);
        OutboxMessageBase[] result = payloads.Select(x => new OutboxMessageBase(x.ToArray())).ToArray();
        return result;
    }
}
