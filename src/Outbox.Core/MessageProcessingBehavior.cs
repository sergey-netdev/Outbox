namespace Outbox.Core;

/// <summary>
/// Defines behavior of messages after successful processing.
/// Note, if a message reaches the maximum retry count it's always moved.
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
