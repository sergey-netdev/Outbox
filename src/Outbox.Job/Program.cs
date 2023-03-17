namespace Outbox.Job;

using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal sealed class Program
{
    private static async Task Main(string[] args)
    {
        IHostBuilder builder = Host.CreateDefaultBuilder(args)
            .ConfigureServices((hostContext, services) =>
            {
                services.AddHostedService<ConsoleHostedService>();
                services.AddMassTransit(x =>
                {
                    x.UsingRabbitMq((IBusRegistrationContext ctx, IRabbitMqBusFactoryConfigurator cfg) =>
                    {
                    });
                });
            });

        await builder.RunConsoleAsync();
    }
}