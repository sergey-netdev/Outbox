namespace Outbox.Core;

/// <summary>
/// Represents a message storage structure in the database.
/// </summary>
public interface IOutboxMessageRow : IOutboxMessage
{
    long SeqNum { get; }
    byte RetryCount { get; }
    DateTimeOffset GeneratedAtUtc { get; }
    DateTimeOffset? ProcessedAtUtc { get; }
    DateTimeOffset? LockedAtUtc { get; }
    DateTimeOffset? LastErrorAtUtc { get; }
}
