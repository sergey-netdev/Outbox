﻿namespace Outbox.Sql;

/// <summary>
/// Defines behavior of messages after successful processing.
/// If a message reaches <see cref="OutboxRepositoryOptions.MaxRetryCount"/> it's always moved.
/// </summary>
public enum MessageProcessingBehavior : byte
{
    /// <summary>
    /// A message is deleted from <c>Outbox</c> table.
    /// </summary>
    Delete = 1,

    /// <summary>
    /// A message is moved from <c>Outbox</c> table to <c>OutboxProcessed</c> table.
    /// It's up to you to decide what to do with it next.
    /// </summary>
    Move = 2,
}
