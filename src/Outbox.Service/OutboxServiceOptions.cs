﻿using Outbox.Core;

namespace Outbox.Service;

public class OutboxServiceOptions : IOutboxServiceOptions
{
    public const string DefaultSectionName = "Outbox";
    public const int DefaultBatchSize = 50;

    /// <summary>
    /// A maximum number of attempts to send a message to the broker.
    /// See <see cref="IOutboxMessageRow.RetryCount"/> and <see cref="IOutboxMessageRow.LastErrorAtUtc"/>.
    /// </summary>
    /// <example>When set to 3, 4 attempts in total will be made to send a message.</example>
    public byte MaxRetryCount { get; set; }

    /// <summary>
    /// How many rows to query for processing in a single go.
    /// </summary>
    public int QueryBatchSize { get; set; } = DefaultBatchSize;

    /// <summary>
    /// How many rows to unlock in a single go.
    /// </summary>
    public int UnlockBatchSize { get; set; } = DefaultBatchSize;

    /// <summary>
    /// How many rows to move in a single go when <see cref="ProcessingBehavior"/> is <see cref="MessageProcessingBehavior.Move"/>.
    /// This is to control pressure to the transaction log.
    /// </summary>
    public int MoveBatchSize { get; set; } = DefaultBatchSize;

    /// <summary>
    /// How many rows to delete in a single go when <see cref="ProcessingBehavior"/> is <see cref="MessageProcessingBehavior.Delete"/>.
    /// This is to control pressure to the transaction log.
    /// </summary>
    public int DeleteBatchSize { get; set; } = DefaultBatchSize;

    /// <summary>
    /// Message lock duration.
    /// When querying messages for processing a lock is put on it to prevent duplicates.
    /// See also <see cref="UnlockInterval"/>.
    /// </summary>
    public TimeSpan LockTimeout { get; set; }

    /// <summary>Default polling interval (normal operation).</summary>
    public TimeSpan ProcessingInterval { get; set; }

    /// <summary>Polling interval to trigger message unlocking. See also: <see cref="LockTimeoutInSeconds"/>.</summary>
    public TimeSpan UnlockInterval { get; set; }

    public MessageProcessingBehavior ProcessingBehavior { get; set; } = MessageProcessingBehavior.Delete;
}
