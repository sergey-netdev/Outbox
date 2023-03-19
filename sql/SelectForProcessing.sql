--declare @BatchSize int = 10;
--declare @MaxRetryCount int = 3;
--declare @LockTimeoutInSeconds int = 120;

declare @RowsToProcess table ( -- the definition must be in sync with dbo.Outbox table
    SeqNum bigint not null,
    MessageId       varchar(36)     not null,
    MessageType     varchar(512)    not null,
    Topic           varchar(128)    not null,
    PartitionId     varchar(32)     null,
    RetryCount      tinyint         not null,
    LockedAtUtc     datetime2       null,
    GeneratedAtUtc  datetime2       not null,
    LastErrorAtUtc  datetime2       null,
    ProcessedAtUtc  datetime2       null,
    Payload         varbinary(max)  null
);

with CTE as (
    select top (@BatchSize)
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
    from
        dbo.Outbox
    where 
        ProcessedAtUtc is null
            and (LockedAtUtc is null) -- or (LockedAtUtc is not null and DATEDIFF(ss, LockedAtUtc, GETUTCDATE()) > @LockTimeoutInSeconds))
            and (RetryCount <= @MaxRetryCount)
    order by
        SeqNum asc -- we want FIFO
)
update CTE set LockedAtUtc = GETUTCDATE()
output
    inserted.SeqNum,
    inserted.MessageId,
    inserted.MessageType,
    inserted.Topic,
    inserted.PartitionId,
    inserted.RetryCount,
    inserted.LockedAtUtc,
    inserted.GeneratedAtUtc,
    inserted.LastErrorAtUtc,
    inserted.ProcessedAtUtc,
    inserted.Payload
into
    @RowsToProcess;

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
from @RowsToProcess;

/*
insert into dbo.Outbox (
    MessageId,
    MessageType,
    Topic,
    PartitionId,
    Payload)
values (
    CAST(NEWID() as varchar(36)),
    'TestMessageType',
    'TestTopic',
    null,
    CONVERT(varbinary(300), '{ "body": "stupid json" }')
);
go 300
*/