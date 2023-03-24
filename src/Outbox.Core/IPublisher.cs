namespace Outbox.Core;

public interface IPublisher
{
    Task PublishAsync(IOutboxMessage message);
}
