select
    SeqNum,
    MessageId,
    MessageType,
    Topic,
    PartitionId,
    RetryCount,
    LockedAtUtc,
    GeneratedAtUtc,
    LastErrorAtUtc,
    ProcessedAtUtc,
    Payload
from dbo.Outbox;
