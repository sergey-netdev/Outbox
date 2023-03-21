namespace Outbox.Service;

using Microsoft.Extensions.Logging;
using Outbox.Core;

public class OutboxService : IOutboxService
{
    private readonly OutboxServiceOptions _outboxServiceOptions;
    private readonly IOutboxRepository _outboxRepository;
    private readonly ILogger _logger;

    public OutboxService(
        OutboxServiceOptions outboxServiceOptions,
        IOutboxRepository outboxRepository,
        ILogger<OutboxService> logger)
    {
        _outboxServiceOptions = outboxServiceOptions ?? throw new ArgumentNullException(nameof(outboxServiceOptions));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task ExecuteAsync(CancellationToken stoppingToken = default)
    {
        _logger.LogInformation("Doing something");
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            int messageCount = int.MaxValue;
            try
            {
                messageCount = await this.TryQueryAndPublishMessagesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex!, string.Empty);
            }

            if (messageCount < _outboxServiceOptions.QueryBatchSize)
            {
                // sleep, if we got less messages than requested, otherwise immediately process the next batch
                await Task.Delay(_outboxServiceOptions.ProcessingInterval, cancellationToken);
            }
        }
    }

    private async Task<int> TryQueryAndPublishMessagesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<IOutboxMessageRow> batch = await _outboxRepository.LockAndGetNextBatchAsync(_outboxServiceOptions.QueryBatchSize, cancellationToken);
        // TODO: publish
        return batch.Count;
    }

    public void Dispose()
    {
        throw new NotImplementedException();
    }
}
