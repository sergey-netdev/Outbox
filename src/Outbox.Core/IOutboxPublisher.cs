namespace Outbox.Core;

public interface IOutboxPublisher
{
    Task PublishAsync(IOutboxMessage message);
}
