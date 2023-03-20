namespace Outbox.Sql;
using Outbox.Core;

public class OutboxRepositoryOptions
{
    public const string DefaultSectionName = "Outbox";

    /// <summary>
    /// Sql Server connection string.
    /// </summary>
    public string? SqlConnectionString { get; set; }

    /// <summary>
    /// A maximum number of attempts to send a message to the broker.
    /// See <see cref="IOutboxMessageRow.RetryCount"/> and <see cref="IOutboxMessageRow.LastErrorAtUtc"/>.
    /// </summary>
    /// <example>When set to 3, 4 attempts in total will be made to send a message.</example>
    public byte MaxRetryCount { get; set; }

    /// <summary>
    /// Message lock duration.
    /// When querying messages for processing a lock is put on it to prevent duplicates.
    /// See also <see cref="UnlockInterval"/>.
    /// </summary>
    public TimeSpan LockTimeout { get; set; }
}
