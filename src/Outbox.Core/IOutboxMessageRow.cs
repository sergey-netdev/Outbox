namespace Outbox.Core;

/// <summary>
/// Represents a message storage structure in the database.
/// </summary>
public interface IOutboxMessageRow : IOutboxMessage
{
    long SeqNum { get; }
    byte RetryCount { get; }
    DateTime GeneratedAtUtc { get; }
    DateTime? ProcessedAtUtc { get; }
    DateTime? LockedAtUtc { get; }
    DateTime? LastErrorAtUtc { get; }
}
