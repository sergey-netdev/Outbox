namespace Outbox.Core;

public interface IOutboxMessage
{
    long SeqNum { get; }
    string MessageId { get; }
    string MessageType { get; }

    string Topic { get; }
    string PartitionId { get; }
    byte RetryCount { get; }

    DateTimeOffset GeneratedAtUtc { get; }
    DateTimeOffset? ProcessedAtUtc { get; }
    DateTimeOffset? LockedAtUtc { get; }
    DateTimeOffset? LastErrorAtUtc { get; }
    byte[] Payload { get; }
}
