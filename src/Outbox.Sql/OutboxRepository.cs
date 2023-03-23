namespace Outbox.Sql;
using Microsoft.Data.SqlClient;
using Outbox.Core;
using System;
using System.Data;
using System.Reflection.PortableExecutable;
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

    public Task<IReadOnlyDictionary<string, long>> PutBatchAsync(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameof(batch));
        //TODO: Create a separate model class, add validation for props & MessageId uniqueness?

        return this.PutBatchAsyncInternal(batch, cancellationToken);
    }

    private async Task<IReadOnlyDictionary<string, long>> PutBatchAsyncInternal(IReadOnlyCollection<IOutboxMessage> batch, CancellationToken cancellationToken = default)
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
            command.Parameters.AddWithValue("@PartitionId", SetNullable(m.PartitionId));
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

    /// <summary>
    /// For testing only. Inserts a row into <c>Outbox</c> table.
    /// </summary>
    /// <param name="rows">A collection to insert. Note, <see cref="IOutboxMessageRow.SeqNum"/> is ignored.</param>
    /// <returns>A map between <see cref="IOutboxMessage.MessageId"/> and <see cref="IOutboxMessageRow.SeqNum"/> generated.</returns>
    internal async Task<IReadOnlyDictionary<string, long>> InsertAsync(IReadOnlyCollection<IOutboxMessageRow> rows, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(nameof(rows));

        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        Dictionary<string, long> result = new(capacity: rows.Count);
        foreach (IOutboxMessageRow row in rows)
        {
            using SqlCommand command = new(SQL.InsertRaw, connection);
            command.Parameters.AddWithValue("@MessageId", row.MessageId);
            command.Parameters.AddWithValue("@MessageType", row.MessageType);
            command.Parameters.AddWithValue("@Topic", row.Topic);
            command.Parameters.AddWithValue("@PartitionId", SetNullable(row.PartitionId));
            command.Parameters.AddWithValue("@Payload", row.Payload);
            command.Parameters.AddWithValue("@RetryCount", row.RetryCount);
            command.Parameters.AddWithValue("@LockedAtUtc", SetNullable(row.LockedAtUtc));
            command.Parameters.AddWithValue("@GeneratedAtUtc", row.GeneratedAtUtc);
            command.Parameters.AddWithValue("@LastErrorAtUtc", SetNullable(row.LastErrorAtUtc));
            command.Parameters.AddWithValue("@ProcessedAtUtc", SetNullable(row.ProcessedAtUtc));

            long seqNum = (long)(await command.ExecuteScalarAsync(cancellationToken));
            result.Add(row.MessageId, seqNum);
        }

        return result;
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

    internal async Task<IReadOnlyCollection<IOutboxMessageRow>> SelectAllAsync(CancellationToken cancellationToken = default)
    {
        using SqlConnection connection = new(_options.SqlConnectionString);
        await connection.OpenAsync(cancellationToken);

        using SqlCommand command = new(SQL.SelectAll, connection);
        using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        List<IOutboxMessageRow> result = new();
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(ReadRow(reader));
        }

        return result;
    }

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

    private static object SetNullable<T>(T? value) => value is null ? DBNull.Value : value;

    ////private static T? GetNullable<T>(IDataReader reader, string name) where T: struct
    ////{
    ////    int indx = reader.GetOrdinal(name);
    ////    return reader.IsDBNull(indx) ? default(T?) : (T)reader[indx];
    ////}
    ////private static T? GetNullable<T>(IDataReader reader, string name) where T : class
    ////{
    ////    int indx = reader.GetOrdinal(name);
    ////    return reader.IsDBNull(indx) ? null : (T)reader[indx];
    ////}

    private static IOutboxMessageRow ReadRow(SqlDataReader reader)
    {
        OutboxMessageRow result = new(
            messageId: (string)reader["MessageId"],
            messageType: (string)reader["MessageType"],
            topic: (string)reader["Topic"],
            payload: (byte[])reader["Payload"])
        {
            SeqNum = (long)reader["SeqNum"],
            PartitionId = reader["PartitionId"] as string,
            RetryCount = (byte)reader["RetryCount"],
            GeneratedAtUtc = (DateTime)reader["GeneratedAtUtc"],
            LockedAtUtc = reader["LockedAtUtc"] as DateTime?,
            ProcessedAtUtc = reader["ProcessedAtUtc"] as DateTime?,
            LastErrorAtUtc = reader["LastErrorAtUtc"] as DateTime?,
        };

        return result;
    }
}
