namespace Outbox.Service.Console;
using Microsoft.Extensions.Hosting;
using Outbox.Core;

public class HostedOutboxUnlockingService : BackgroundService
{
    private readonly IOutboxService _outboxService;

    public HostedOutboxUnlockingService(IOutboxService outboxService)
    {
        _outboxService = outboxService ?? throw new ArgumentNullException(nameof(outboxService));
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken) => _outboxService.RunAsync(stoppingToken);

    public override void Dispose()
    {
        _outboxService.Dispose();
        GC.SuppressFinalize(this);
    }
}
