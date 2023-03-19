with CTE as (
    select top (@BatchSize) *
    from dbo.Outbox
    where
        LockedAtUtc is not null and DATEDIFF(ss, LockedAtUtc, GETUTCDATE()) > @LockTimeoutInSeconds
    order by SeqNum
)
update CTE set LockedAtUtc = null
