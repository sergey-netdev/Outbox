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
    private readonly IPublisher _publisher;

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
        _publisher = _host.Services.GetRequiredService<IPublisher>();
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
    }
}
