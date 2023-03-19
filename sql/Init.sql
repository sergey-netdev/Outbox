drop table if exists Outbox;
if not exists (select * from sysobjects where name='Outbox' and xtype='u')
    create table [dbo].[Outbox] (
        SeqNum          bigint          not null        identity,
            constraint [PK_dbo.Outbox]                  primary key clustered (SeqNum ASC),
        MessageId       varchar(36)     not null
            constraint [CC_dbo.Outbox_MessageId]        check (MessageId <> ''),
        MessageType     varchar(512)    not null
            constraint [CC_dbo.Outbox_MessageType]      check (MessageType <> ''),
        Topic           varchar(128)    not null,
        PartitionId     varchar(32)     null,
        RetryCount      tinyint         not null
            constraint [DC_dbo.Outbox_RetryCount]       default 0,
        GeneratedAtUtc  datetime2       not null
            constraint [DC_dbo.Outbox_GeneratedAtUtc]   default GETUTCDATE(),
        LockedAtUtc     datetime2       null,
        ProcessedAtUtc  datetime2       null,
        LastErrorAtUtc  datetime2       null,

        Payload         varbinary(max)  null,

        constraint [CC_dbo.Outbox_ProcessedAtUtc]       check (ProcessedAtUtc is null or ProcessedAtUtc > GeneratedAtUtc),
        constraint [CC_dbo.Outbox_LastErrorAtUtc]       check (LastErrorAtUtc is null or LastErrorAtUtc > GeneratedAtUtc),
   );
-- select * from dbo.Outbox
