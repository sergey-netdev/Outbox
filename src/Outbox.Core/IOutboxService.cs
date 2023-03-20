namespace Outbox.Core;

public interface IOutboxService : IDisposable
{
    Task RunAsync(CancellationToken cancellationToken = default);
}