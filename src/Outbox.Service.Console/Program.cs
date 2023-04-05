namespace Outbox.Service.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Outbox.Publisher.RabbitMQ;
using Outbox.Sql;
using System.Text.Json;

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
            .ConfigureLogging(builder =>
                builder.AddSimpleConsole(options =>
                {
                    options.IncludeScopes = false;
                    options.TimestampFormat = "HH:mm:ss ";
                }))
            .ConfigureServices(services =>
            {
                services.AddOutboxSqlRepository(configuration);
                services.AddOutboxRabbitMQPublisher(configuration);
                services.AddOutboxService(configuration);

                services.AddHostedService<HostedOutboxPublishingService>();
                services.AddHostedService<HostedOutboxUnlockingService>();
            });

        IHost host = hostBuilder.Build();
        await host.RunAsync();
    }
}

