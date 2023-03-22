insert into dbo.Outbox (
    MessageId,
    MessageType,
    Topic,
    PartitionId,
    Payload,
    RetryCount,
    LockedAtUtc,
    GeneratedAtUtc,
    LastErrorAtUtc,
    ProcessedAtUtc
) values (
    @MessageId,
    @MessageType,
    @Topic,
    @PartitionId,
    @Payload,
    @RetryCount,
    @LockedAtUtc,
    @GeneratedAtUtc,
    @LastErrorAtUtc,
    @ProcessedAtUtc
);

select CONVERT(bigint, SCOPE_IDENTITY());
