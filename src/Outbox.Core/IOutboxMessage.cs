namespace Outbox.Core;

/// <summary>
/// Represents a message to be published.
/// </summary>
public interface IOutboxMessage
{
    string MessageId { get; }
    string MessageType { get; }
    string Topic { get; }
    string? PartitionId { get; }
    byte[] Payload { get; }
}
