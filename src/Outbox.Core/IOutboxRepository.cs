namespace Outbox.Core;

public interface IOutboxRepository
{
    Task<IReadOnlyCollection<IOutboxMessage>> GetNextBatchAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default);
}
