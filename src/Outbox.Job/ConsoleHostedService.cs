namespace Outbox.Job;

using MassTransit;
using Microsoft.Extensions.Hosting;
using System.Text.Json;

internal class ConsoleHostedService : IHostedService
{
    private readonly IPublishEndpoint _publishEndpoint;

    public ConsoleHostedService(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint ?? throw new ArgumentNullException(nameof(publishEndpoint));
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        object message = JsonSerializer.Serialize(new { Title = "Test note", Body = "This is a note body." });

        await _publishEndpoint.Publish(message, cancellationToken).ConfigureAwait(false);

        // poll sql
        await Task.Delay(0);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(0);
    }
}
