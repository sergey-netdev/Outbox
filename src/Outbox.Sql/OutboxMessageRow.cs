namespace Outbox.Sql;
using Outbox.Core;

public sealed class OutboxMessageRow : OutboxMessage, IOutboxMessageRow
{
    public OutboxMessageRow(string messageId, string messageType, string topic, byte[] payload)
        :base(messageId, messageType, topic, payload)
    {
    }

    public long SeqNum { get; set; }

    public byte RetryCount { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime? ProcessedAtUtc { get; set; }

    public DateTime? LockedAtUtc { get; set; }

    public DateTime? LastErrorAtUtc { get; set; }
}
