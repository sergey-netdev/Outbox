namespace Outbox.Core;

/// <summary>
/// Represents a message you get on a receiving side.
/// </summary>
public interface IOutboxMessageBase
{
    byte[] Payload { get; }
}
