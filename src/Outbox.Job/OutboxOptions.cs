namespace Outbox.Sql;

public class OutboxOptions
{
    public const string DefaultSectionName = "Outbox";

    public string? SqlConnectionString { get; set; }

    public int QueryBatchSize { get; set; }
    public byte? MaxRetryCount { get; set; }
    public int LockTimeoutInSeconds { get; set; }
}
