using Outbox.Core;

namespace Outbox.Job
{
    public sealed class OutboxMessage : IOutboxMessage
    {
        public OutboxMessage(string messageId, string messageType, string topic, string partitionId, byte[] payload)
        {
            this.MessageId = messageId ?? throw new ArgumentNullException(nameof(messageId));
            this.MessageType = messageType ?? throw new ArgumentNullException(nameof(messageType));
            this.Topic = topic ?? throw new ArgumentNullException(nameof(topic));
            this.PartitionId = partitionId ?? throw new ArgumentNullException(nameof(partitionId));
            this.Payload = payload ?? throw new ArgumentNullException(nameof(payload));
        }

        public long SeqNum { get; set; }

        public string MessageId { get; set; }

        public string MessageType { get; set; }

        public string Topic { get; set; }

        public string PartitionId { get; set; }

        public byte RetryCount { get; set; }

        public DateTimeOffset GeneratedAtUtc { get; set; }

        public DateTimeOffset? ProcessedAtUtc { get; set; }

        public DateTimeOffset? LockedAtUtc { get; set; }

        public DateTimeOffset? LastErrorAtUtc { get; set; }

        public byte[] Payload { get; set; }
    }
}
