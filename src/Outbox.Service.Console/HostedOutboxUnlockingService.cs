namespace Outbox.Service.Console;
using Microsoft.Extensions.Logging;
using Outbox.Core;

public class HostedOutboxUnlockingService : HostedOutboxServiceBase
{
    private readonly IOutboxService _outboxService;

    public HostedOutboxUnlockingService(
        IOutboxService outboxService,
        ILogger<HostedOutboxUnlockingService> logger)
        : base(outboxService.UnlockAsync, outboxService.Options.UnlockBatchSize, outboxService.Options.UnlockInterval, logger)
    {
        _outboxService = outboxService;
    }

    public override void Dispose()
    {
        _outboxService.Dispose();
        GC.SuppressFinalize(this);
    }
}
