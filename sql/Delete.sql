with CTE as (
    select top (@BatchSize) *
    from dbo.Outbox
    where
        ProcessedAtUtc is not null or RetryCount <= @MaxRetryCount
    order by SeqNum
)
delete from CTE
