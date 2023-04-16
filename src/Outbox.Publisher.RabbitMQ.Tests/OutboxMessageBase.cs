namespace Outbox.Publisher.RabbitMQ.Tests;
using Outbox.Core;

public class OutboxMessageBase : IOutboxMessageBase
{
    public OutboxMessageBase(byte[] payload)
    {
        this.Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public byte[] Payload { get; set; }
}
