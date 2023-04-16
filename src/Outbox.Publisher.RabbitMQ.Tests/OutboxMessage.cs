namespace Outbox.Publisher.RabbitMQ.Tests;
using Outbox.Core;

public class OutboxMessage : OutboxMessageBase, IOutboxMessage
{
    public OutboxMessage(string messageId, string messageType, string topic, byte[] payload)
        : base(payload)
    {
        this.MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        this.MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        this.Topic = topic ?? throw new ArgumentNullException(nameof(topic));
    }

    public string MessageId {get; set;}

    public string MessageType { get; set; }

    public string Topic { get; set; }

    public string? PartitionId { get; set; }
}
