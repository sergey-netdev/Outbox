--declare @SeqNum bigint = 9;
--declare @MaxRetryCount tinyint = 2;
--declare @RetryCount tinyint;
-- select * from dbo.Outbox
-- select * from dbo.OutboxProcessed
--update dbo.Outbox set RetryCount = 2 where SeqNum=9

begin tran

declare @RetryCount tinyint;
update dbo.Outbox set
    LastErrorAtUtc = GETUTCDATE(),
    RetryCount = RetryCount + 1,
    @RetryCount = RetryCount + 1
where SeqNum = @SeqNum;

if @RetryCount > @MaxRetryCount
begin

insert into dbo.OutboxProcessed (
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
)
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
from dbo.Outbox
where SeqNum = @SeqNum;

delete from dbo.Outbox
where SeqNum = @SeqNum;
end;

commit tran;
