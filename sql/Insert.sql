insert into dbo.Outbox (
    MessageId,
    MessageType,
    Topic,
    PartitionId,
    Payload
) values (
    @MessageId,
    @MessageType,
    @Topic,
    @PartitionId,
    @Payload
);

select CONVERT(bigint, SCOPE_IDENTITY());
