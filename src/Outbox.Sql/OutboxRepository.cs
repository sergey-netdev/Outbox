namespace Outbox.Sql;

using Microsoft.Data.SqlClient;
using Outbox.Core;
using System.Threading;

public class OutboxRepository : IOutboxRepository
{
    private readonly OutboxOptions _options;

    public OutboxRepository(OutboxOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    public async Task<IReadOnlyCollection<IOutboxMessage>> LockAndGetNextBatchAsync(CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.SelectForProcessing, connection);
        command.Parameters.AddWithValue("@BatchSize", _options.QueryBatchSize);
        command.Parameters.AddWithValue("@MaxRetryCount", _options.MaxRetryCount);
        command.Parameters.AddWithValue("@LockTimeoutInSeconds", _options.LockTimeoutInSeconds);

        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        List<IOutboxMessage> result = new(_options.QueryBatchSize); // allocate in advance as it's likely we'll have full batches every time under high load
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadRow(reader));
        }

        return result;
    }

    public Task UnlockAsync(CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameof(batch));
        //TODO: Create a separate model class, add validation for props & MessageId uniqueness?

        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        Dictionary<string, long> keys = new(capacity: batch.Count);

        foreach (IOutboxMessage entry in batch)
        {
            using SqlCommand command = new(SQL.Insert, connection);
            command.Parameters.AddWithValue("@MessageId", entry.MessageId);
            command.Parameters.AddWithValue("@MessageType", entry.MessageType);
            command.Parameters.AddWithValue("@Topic", entry.Topic);
            command.Parameters.AddWithValue("@PartitionId", entry.PartitionId);
            command.Parameters.AddWithValue("@Payload", entry.Payload);

            long seqNum = (long)(await command.ExecuteScalarAsync(cancellationToken));
            keys.Add(entry.MessageId, seqNum);
        }

        return keys;
    }

    /// <summary>
    /// For testing only. Truncates the storage table.
    /// </summary>
    internal async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.Truncate, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static IOutboxMessage ReadRow(SqlDataReader reader)
    {
        OutboxMessage result = new(
            messageId: (string)reader["MessageId"],
            messageType: (string)reader["MessageType"],
            topic: (string)reader["Topic"],
            partitionId: (string)reader["PartitionId"],
            payload: (byte[])reader["Payload"])
        {
            SeqNum = (long)reader["SeqNum"],
            RetryCount = (byte)reader["RetryCount"],
            GeneratedAtUtc = reader.GetFieldValue<DateTime>(reader.GetOrdinal("GeneratedAtUtc")).ToUniversalTime(),
            LockedAtUtc = GetNullable<DateTime>("LockedAtUtc")?.ToUniversalTime(),
            ProcessedAtUtc = GetNullable<DateTime>("ProcessedAtUtc")?.ToUniversalTime(),
            LastErrorAtUtc = GetNullable<DateTime>("LastErrorAtUtc")?.ToUniversalTime(),
        };

        return result;

        T? GetNullable<T>(string name) where T : struct
        {
            int indx = reader.GetOrdinal(name);
            return reader.IsDBNull(indx) ? default(T) : (T)reader[indx];
        }
    }
}
