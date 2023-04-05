namespace Outbox.Core;

public interface IOutboxService : IDisposable
{
    IOutboxServiceOptions Options { get; }

    Task<int> PublishAsync(int batchSize, CancellationToken cancellationToken = default);

    Task<int> UnlockAsync(int batchSize, CancellationToken cancellationToken = default);
}
