namespace Outbox.Service.Console;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting; // Requires NuGet package
using Microsoft.Extensions.Logging;
using Outbox.Core;
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
                OutboxServiceOptions outboxServiceOptions = configuration.GetSection(OutboxServiceOptions.DefaultSectionName).Get<OutboxServiceOptions>()
                    ?? throw new InvalidOperationException($"Cannot initialize configuration.");
                services.AddSingleton(outboxServiceOptions);

                OutboxRepositoryOptions outboxRepositoryOptions = configuration.GetSection(OutboxRepositoryOptions.DefaultSectionName).Get<OutboxRepositoryOptions>()
                    ?? throw new InvalidOperationException($"Cannot initialize configuration.");
                services.AddSingleton(outboxRepositoryOptions);

                services.AddSingleton<IOutboxRepository, OutboxRepository>();
                services.AddSingleton<OutboxService>();
            });

        IHost host = hostBuilder.Build();
        OutboxService outboxService = host.Services.GetRequiredService<OutboxService>();
        await outboxService.ExecuteAsync();
    }
}

