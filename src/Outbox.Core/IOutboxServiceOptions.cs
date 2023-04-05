namespace Outbox.Core;

public interface IOutboxServiceOptions
{
    /// <summary>
    /// A maximum number of attempts to send a message to the broker.
    /// See <see cref="IOutboxMessageRow.RetryCount"/> and <see cref="IOutboxMessageRow.LastErrorAtUtc"/>.
    /// </summary>
    /// <example>When set to 3, 4 attempts in total will be made to send a message.</example>
    byte MaxRetryCount { get; }

    /// <summary>
    /// How many rows to query for processing in a single go.
    /// </summary>
    int QueryBatchSize { get; }

    /// <summary>
    /// How many rows to unlock in a single go.
    /// </summary>
    int UnlockBatchSize { get; }

    /// <summary>
    /// How many rows to move in a single go when <see cref="ProcessingBehavior"/> is <see cref="MessageProcessingBehavior.Move"/>.
    /// This is to control pressure to the transaction log.
    /// </summary>
    int MoveBatchSize { get; }

    /// <summary>
    /// How many rows to delete in a single go when <see cref="ProcessingBehavior"/> is <see cref="MessageProcessingBehavior.Delete"/>.
    /// This is to control pressure to the transaction log.
    /// </summary>
    int DeleteBatchSize { get; }

    /// <summary>
    /// Message lock duration.
    /// When querying messages for processing a lock is put on it to prevent duplicates.
    /// See also <see cref="UnlockInterval"/>.
    /// </summary>
    TimeSpan LockTimeout { get; }

    /// <summary>Default polling interval (normal operation).</summary>
    TimeSpan ProcessingInterval { get; }

    /// <summary>Polling interval to trigger message unlocking. See also: <see cref="LockTimeoutInSeconds"/>.</summary>
    TimeSpan UnlockInterval { get; }

    /// <inheritdoc cref="MessageProcessingBehavior"/>
    MessageProcessingBehavior ProcessingBehavior { get; }
}
