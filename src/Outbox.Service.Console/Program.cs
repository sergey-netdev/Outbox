namespace Outbox.Service.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; // Requires NuGet package
using Microsoft.Extensions.Logging;
using Outbox.Core;
using Outbox.Publisher.RabbitMQ;
using Outbox.Sql;

class Program
{
    public static async Task Main(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
           .AddEnvironmentVariables()
           .AddCommandLine(args)
           .AddJsonFile("appsettings.json")
           .Build();

        IHostBuilder hostBuilder = Host.CreateDefaultBuilder(args)
            .ConfigureAppConfiguration((HostBuilderContext _, IConfigurationBuilder configurationBuilder) =>
            {
                configurationBuilder.Sources.Clear();
                configurationBuilder.AddConfiguration(configuration);
            })
            .ConfigureServices(services =>
            {
                services.AddOutboxSqlRepository(configuration);
                services.AddOutboxRabbitMQPublisher(configuration);
                services.AddOutboxService(configuration);
                services.AddHostedService<HostedOutboxPublishingService>();
            });

        IHost host = hostBuilder.Build();
        await host.RunAsync();

//        OutboxService outboxService = host.Services.GetRequiredService<OutboxService>();
//        await outboxService.ExecuteAsync();
    }
}

