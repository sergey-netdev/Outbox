namespace Outbox.Sql;
using Microsoft.Data.SqlClient;
using Outbox.Core;
using System;
using System.Threading;

public class OutboxRepository : IOutboxRepository
{
    private readonly OutboxRepositoryOptions _options;

    public OutboxRepository(OutboxRepositoryOptions options)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
    }

    internal OutboxRepositoryOptions Options => _options;

    public async Task<IReadOnlyCollection<IOutboxMessageRow>> LockAndGetNextBatchAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.SelectForProcessing, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@MaxRetryCount", _options.MaxRetryCount);
        command.Parameters.AddWithValue("@LockDurationInSeconds", (int)_options.LockDuration.TotalSeconds);

        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        List<IOutboxMessageRow> result = new(batchSize); // allocate in advance as it's likely we'll have full batches every time under high load
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadRow(reader));
        }

        return result;
    }

    public Task<int> UnlockAsync(int batchSize, CancellationToken cancellationToken = default)
    {
        if (batchSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(batchSize), batchSize, "Must be positive value.");
        }

        return this.UnlockAsyncInternal(batchSize, cancellationToken);
    }

    private async Task<int> UnlockAsyncInternal(int batchSize, CancellationToken cancellationToken)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.Unlock, connection);
        command.Parameters.AddWithValue("@BatchSize", batchSize);
        command.Parameters.AddWithValue("@LockDurationInSeconds", (int)_options.LockDuration.TotalSeconds);

        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameof(batch));
        //TODO: Create a separate model class, add validation for props & MessageId uniqueness?

        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        Dictionary<string, long> keys = new(capacity: batch.Count);

        foreach (IOutboxMessage m in batch)
        {
            using SqlCommand command = new(SQL.InsertDefault, connection);
            command.Parameters.AddWithValue("@MessageId", m.MessageId);
            command.Parameters.AddWithValue("@MessageType", m.MessageType);
            command.Parameters.AddWithValue("@Topic", m.Topic);
            command.Parameters.AddWithValue("@PartitionId", m.PartitionId);
            command.Parameters.AddWithValue("@Payload", m.Payload);

            long seqNum = (long)(await command.ExecuteScalarAsync(cancellationToken));
            keys.Add(m.MessageId, seqNum);
        }

        return keys;
    }

    public async Task UpdateMessageAsSuccessfulAsync(long seqNum, bool move, CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.UpdateSuccessful, connection);
        command.Parameters.AddWithValue("@SeqNum", seqNum);
        command.Parameters.AddWithValue("@Move", move);

        await command.ExecuteScalarAsync(cancellationToken);
    }

    public async Task UpdateMessageAsUnsuccessfulAsync(long seqNum, CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.UpdateUnsuccessful, connection);
        command.Parameters.AddWithValue("@SeqNum", seqNum);
        command.Parameters.AddWithValue("@MaxRetryCount", _options.MaxRetryCount);

        await command.ExecuteScalarAsync(cancellationToken);
    }

    /// <summary>
    /// For testing only. Truncates the storage tables.
    /// </summary>
    internal async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.Truncate, connection);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    internal async Task<long> InsertAsync(IOutboxMessageRow row, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameof(row));

        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.InsertRaw, connection);
        command.Parameters.AddWithValue("@MessageId", row.MessageId);
        command.Parameters.AddWithValue("@MessageType", row.MessageType);
        command.Parameters.AddWithValue("@Topic", row.Topic);
        command.Parameters.AddWithValue("@PartitionId", row.PartitionId);
        command.Parameters.AddWithValue("@Payload", row.Payload);
        command.Parameters.AddWithValue("@RetryCount", row.RetryCount);
        command.Parameters.AddWithValue("@LockedAtUtc", row.LockedAtUtc);
        command.Parameters.AddWithValue("@GeneratedAtUtc", row.GeneratedAtUtc);
        command.Parameters.AddWithValue("@LastErrorAtUtc", row.LastErrorAtUtc);
        command.Parameters.AddWithValue("@ProcessedAtUtc", row.ProcessedAtUtc);

        long seqNum = (long)(await command.ExecuteScalarAsync(cancellationToken));
        return seqNum;
    }

    /// <summary>
    /// Selects a single entry by <paramref name="seqNum"/> from <c>Outbox</c> table.
    /// </summary>
    /// <returns><c>null</c> is no entry found.</returns>
    internal Task<IOutboxMessageRow?> SelectAsync(long seqNum, CancellationToken cancellationToken = default) =>
        this.SelectAsync(SQL.Select, seqNum, cancellationToken);

    /// <summary>
    /// Selects a single entry by <paramref name="seqNum"/> from <c>OutboxProcessed</c> table.
    /// </summary>
    /// <returns><c>null</c> is no entry found.</returns>
    internal Task<IOutboxMessageRow?> SelectProcessedAsync(long seqNum, CancellationToken cancellationToken = default) =>
        this.SelectAsync(SQL.SelectProcessed, seqNum, cancellationToken);

    private async Task<IOutboxMessageRow?> SelectAsync(string query, long seqNum, CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(query, connection);
        command.Parameters.AddWithValue("@SeqNum", seqNum);

        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        if (await reader.ReadAsync(cancellationToken))
        { 
            return ReadRow(reader);
        }

        return null;
    }

    private static IOutboxMessageRow ReadRow(SqlDataReader reader)
    {
        OutboxMessageRow result = new(
            messageId: (string)reader["MessageId"],
            messageType: (string)reader["MessageType"],
            topic: (string)reader["Topic"],
            payload: (byte[])reader["Payload"])
        {
            SeqNum = (long)reader["SeqNum"],
            PartitionId = (string)reader["PartitionId"],
            RetryCount = (byte)reader["RetryCount"],
            GeneratedAtUtc = reader.GetFieldValue<DateTime>(reader.GetOrdinal("GeneratedAtUtc")),
            LockedAtUtc = GetNullable<DateTime>("LockedAtUtc"),
            ProcessedAtUtc = GetNullable<DateTime>("ProcessedAtUtc"),
            LastErrorAtUtc = GetNullable<DateTime>("LastErrorAtUtc"),
        };

        return result;

        T? GetNullable<T>(string name) where T : struct
        {
            int indx = reader.GetOrdinal(name);
            return reader.IsDBNull(indx) ? null : (T)reader[indx];
        }
    }
}
