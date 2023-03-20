namespace Outbox.Sql;

/// <summary>
/// Defines behavior of messages after successful processing
/// or when reaching <see cref="OutboxOptions.MaxRetryCount"/>.
/// </summary>
public enum MessageProcessingBehavior : byte
{
    /// <summary>
    /// Do nothing. A message stays in <c>Outbox</c> table.
    /// Not recommended for production use.
    /// </summary>
    None = 0,

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
