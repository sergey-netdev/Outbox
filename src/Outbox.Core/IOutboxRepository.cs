namespace Outbox.Core;

public interface IOutboxRepository
{
    Task<IReadOnlyCollection<IOutboxMessageRow>> LockAndGetNextBatchAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<int> UnlockAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default);

    Task UpdateMessageAsSuccessfulAsync(long seqNum, bool move, CancellationToken cancellationToken = default);

    Task UpdateMessageAsUnsuccessfulAsync(long seqNum, CancellationToken cancellationToken = default);
}
