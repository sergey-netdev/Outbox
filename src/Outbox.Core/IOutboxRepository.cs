namespace Outbox.Core;

public interface IOutboxRepository
{
    Task<IReadOnlyCollection<IOutboxMessageRow>> LockAndGetNextBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    Task UnlockAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default);
}
