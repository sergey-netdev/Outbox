namespace Outbox.Sql;
using Microsoft.Extensions.Hosting;

internal class ConsoleHostedService : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(0, cancellationToken);
    }
}
