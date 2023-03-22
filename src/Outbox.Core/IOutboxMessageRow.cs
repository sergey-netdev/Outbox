namespace Outbox.Core;

/// <summary>
/// Represents a message storage structure in the database.
/// </summary>
public interface IOutboxMessageRow : IOutboxMessage
{
    /// <summary>
    /// A unique auto-increasing value that serves as a primary key.
    /// </summary>
    long SeqNum { get; }

    /// <summary>
    /// Tracks a number of attempts before a message is considered poisonous.
    /// If your maximum retry count is set to <c>3</c> the property will be <c>4</c>
    /// when it's moved to DLQ (at least one attempt to deliver is always made).
    /// </summary>
    byte RetryCount { get; }

    DateTime GeneratedAtUtc { get; }

    DateTime? ProcessedAtUtc { get; }

    DateTime? LockedAtUtc { get; }

    DateTime? LastErrorAtUtc { get; }
}
