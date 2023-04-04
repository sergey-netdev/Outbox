--declare @SeqNum bigint = 8;
--declare @Move bit = 1;
--declare @MaxRetryCount tinyint = 2;
--declare @RetryCount tinyint;
-- select * from dbo.Outbox
-- select * from dbo.OutboxProcessed
--update dbo.Outbox set RetryCount = 2 where SeqNum=9

begin tran;

if @Move = 1
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
        GETUTCDATE(),
        Payload
    from dbo.Outbox
    where SeqNum = @SeqNum;

    delete from dbo.Outbox where SeqNum = @SeqNum;
end else
if (@Move = 0)
begin
    delete from dbo.Outbox where SeqNum = @SeqNum;
end else
if (@Move is null)
begin
    update dbo.Outbox set ProcessedAtUtc = GETUTCDATE() where SeqNum = @SeqNum;
end;

commit tran;
