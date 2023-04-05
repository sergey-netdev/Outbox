namespace Outbox.Service.Console;
using Microsoft.Extensions.Logging;
using Outbox.Core;

public class HostedOutboxPublishingService : HostedOutboxServiceBase
{
    private readonly IOutboxService _outboxService;

    public HostedOutboxPublishingService(
        IOutboxService outboxService,
        ILogger<HostedOutboxPublishingService> logger)
        : base(outboxService.PublishAsync, outboxService.Options.QueryBatchSize, outboxService.Options.ProcessingInterval, logger)
    {
        _outboxService = outboxService;
    }

    public override void Dispose()
    {
        _outboxService.Dispose();
        GC.SuppressFinalize(this);
    }
}
