namespace Outbox.Core;

public interface IOutboxRepository
{
    Task<IReadOnlyCollection<IOutboxMessageRow>> LockAndGetNextBatchAsync(CancellationToken cancellationToken = default);

    Task UnlockAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default);
}
