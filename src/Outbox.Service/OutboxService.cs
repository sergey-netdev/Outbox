namespace Outbox.Service;

using Microsoft.Extensions.Logging;
using Outbox.Core;

public class OutboxService : IOutboxService
{
    private readonly IOutboxServiceOptions _outboxServiceOptions;
    private readonly IOutboxRepository _outboxRepository;
    private readonly IOutboxPublisher _publisher;
    private readonly ILogger _logger;

    public OutboxService(
        IOutboxServiceOptions outboxServiceOptions,
        IOutboxRepository outboxRepository,
        IOutboxPublisher publisher,
        ILogger<OutboxService> logger)
    {
        _outboxServiceOptions = outboxServiceOptions ?? throw new ArgumentNullException(nameof(outboxServiceOptions));
        _outboxRepository = outboxRepository ?? throw new ArgumentNullException(nameof(outboxRepository));
        _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public IOutboxServiceOptions Options => _outboxServiceOptions;

    ////public Task PublishAsync(CancellationToken cancellationToken = default)
    ////{
    ////    return OutboxServiceBase.RunAsync(_logger, _outboxServiceOptions.QueryBatchSize, _outboxServiceOptions.ProcessingInterval,
    ////        TryQueryAndPublishMessagesAsync, cancellationToken);
    ////}

    ////public Task UnlockAsync(CancellationToken cancellationToken = default)
    ////{
    ////    return OutboxServiceBase.RunAsync(_logger, _outboxServiceOptions.UnlockBatchSize, _outboxServiceOptions.UnlockInterval,
    ////        TryQueryAndPublishMessagesAsync, cancellationToken);
    ////}

    public Task<int> UnlockAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        return _outboxRepository.UnlockAsync(batchSize, cancellationToken);
    }

    public async Task<int> PublishAsync(int batchSize, CancellationToken cancellationToken)
    {
        IReadOnlyCollection<IOutboxMessageRow> batch = await _outboxRepository.LockAndGetNextBatchAsync(batchSize, cancellationToken);
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
                ////case MessageProcessingBehavior.None:
                ////    break;
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
