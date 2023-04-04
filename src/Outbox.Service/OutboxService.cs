namespace Outbox.Service;

using Microsoft.Extensions.Logging;
using Outbox.Core;

public class OutboxService : IOutboxService
{
    private readonly OutboxServiceOptions _outboxServiceOptions;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxPublisher _publisher;
    private readonly ILogger _logger;

    public OutboxService(
        OutboxServiceOptions outboxServiceOptions,
        IOutboxRepository outboxRepository,
        IOutboxPublisher publisher,
        ILogger<OutboxService> logger)
    {
        _outboxServiceOptions = outboxServiceOptions ?? throw new ArgumentNullException(nameof(outboxServiceOptions));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public Task ExecuteAsync(CancellationToken cancellationToken = default)
    {
        return RunAsync(cancellationToken);
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Entering the main loop.");

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
                _logger.LogTrace("Sleeping...");
                await Task.Delay(_outboxServiceOptions.ProcessingInterval, cancellationToken);
            }
        }
    }

    private async Task<int> TryQueryAndPublishMessagesAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<IOutboxMessageRow> batch = await _outboxRepository.LockAndGetNextBatchAsync(_outboxServiceOptions.QueryBatchSize, cancellationToken);
        _logger.LogInformation("Got {messageCount} messages", batch.Count);

        foreach (var message in batch)
        {
            try
            {
                await _publisher.PublishAsync(message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error publishing message.");
                await _outboxRepository.UpdateMessageAsUnsuccessfulAsync(message.SeqNum, cancellationToken);
                continue;
            }

            switch (_outboxServiceOptions.ProcessingBehavior)
            {
                case MessageProcessingBehavior.Delete:
                    await _outboxRepository.UpdateMessageAsSuccessfulAsync(message.SeqNum, move: false, cancellationToken);
                    break;
                case MessageProcessingBehavior.Move:
                    await _outboxRepository.UpdateMessageAsSuccessfulAsync(message.SeqNum, move: true, cancellationToken);
                    break;
                case MessageProcessingBehavior.None:
                    break;
                default:
                    throw new NotImplementedException($"Unknown {nameof(MessageProcessingBehavior)}: {_outboxServiceOptions.ProcessingBehavior}.");
            }

        }

        return batch.Count;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
    }
}
