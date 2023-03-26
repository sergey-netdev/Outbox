namespace Outbox.Tests;
using Outbox.Core;

public class OutboxMessage : IOutboxMessage
{
    public OutboxMessage(string messageId, string messageType, string topic, byte[] payload)
    {
        this.MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
        this.MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
        this.Topic = topic ?? throw new ArgumentNullException(nameof(topic));
        this.Payload = payload ?? throw new ArgumentNullException(nameof(payload));
    }

    public string MessageId {get; set;}

    public string MessageType { get; set; }

    public string Topic { get; set; }

    public string? PartitionId { get; set; }

    public byte[] Payload { get; set; }
}
