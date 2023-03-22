namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Outbox.Core;
using Outbox.Sql;
using System.Text;
using System.Text.Json;

[Collection("Sequential")]
public class OutboxRepositoryTests : IAsyncLifetime
{
    const int QueryBatchSize = 10;
    private readonly OutboxRepository _repository;
    private readonly OutboxRepositoryOptions options = new();

    public OutboxRepositoryTests()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();
        configuration.GetSection(OutboxRepositoryOptions.DefaultSectionName).Bind(options);

        _repository = new OutboxRepository(options);
    }

    #region IAsyncLifetime
    public async Task InitializeAsync()
    {
        await _repository.ClearAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;
    #endregion

    [Theory]
    [InlineData(QueryBatchSize)]
    [InlineData(QueryBatchSize * 2)]
    [InlineData(QueryBatchSize + 2)]
    [InlineData(QueryBatchSize - 2)]
    public async Task LockAndGetNextBatchAsync_Returns_Specified_Number_Of_Message_Rows(int totalRows)
    {
        // setup
        OutboxMessage[] batch = GenerateRndMessages(totalRows).ToDictionary(x => x.MessageId, x => x).Values.ToArray();
        await _repository.PutBatchAsync(batch);

        // act #1
        int expectedRows = Math.Min(QueryBatchSize, totalRows);
        IReadOnlyCollection<IOutboxMessageRow> rows1 = await LockAndGetNextBatchAsync(expectedRows);

        // act #2 (must return a different set)
        expectedRows = Math.Min(QueryBatchSize, totalRows - expectedRows);
        IReadOnlyCollection<IOutboxMessageRow> rows2 = await LockAndGetNextBatchAsync(expectedRows);

        // verify
        Assert.NotEqual(rows1.Select(x => x.SeqNum), rows2.Select(x => x.SeqNum));

        async Task<IReadOnlyCollection<IOutboxMessageRow>> LockAndGetNextBatchAsync(int expectedRows)
        {
            IReadOnlyCollection<IOutboxMessageRow> rows = await _repository.LockAndGetNextBatchAsync(QueryBatchSize);

            // verify
            Assert.NotNull(rows);
            Assert.Equal(expectedRows, rows.Count);
            Assert.Equal(rows.Count, rows.Select(x => x.MessageId).Distinct().Count());

            foreach (IOutboxMessageRow row in rows)
            {
                Assert.Equal(0, row.RetryCount);
                Assert.NotNull(row.MessageId);
                Assert.NotNull(row.MessageType);
                Assert.Null(row.LastErrorAtUtc);
                Assert.NotNull(row.LockedAtUtc);
                Assert.Null(row.ProcessedAtUtc);
            }

            return rows;
        }
    }

    [Fact]
    public async Task PutBatchAsync_Inserts_Message_Rows()
    {
        IReadOnlyDictionary<string, OutboxMessage> batch = GenerateRndMessages(QueryBatchSize).ToDictionary(x => x.MessageId, x => x);

        IReadOnlyDictionary<string, long> keys = await _repository.PutBatchAsync(batch.Values.ToArray());

        // verify
        Assert.NotNull(keys);
        Assert.Equal(QueryBatchSize, keys.Count);
        Assert.Equal(QueryBatchSize, keys.Values.Distinct().Count());
        Assert.Equal(batch.Select(x => x.Key), keys.Keys);

        // verify every message
        foreach (KeyValuePair<string, long> key in keys)
        {
            IOutboxMessageRow? row = await _repository.SelectAsync(seqNum: key.Value);
            Assert.NotNull(row);

            OutboxMessage expectedMessage = batch[key.Key];
            VerifyNew(expectedMessage, row);
        }
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task UpdateMessageAsSuccessfulAsync_Deletes_Or_Moves_A_Message(bool move)
    {
        OutboxMessage message = GenerateRndMessage();
        long seqNum = (await _repository.PutBatchAsync(new[] { message })).Single().Value;

        await _repository.SelectAsync(seqNum);

        await _repository.UpdateMessageAsSuccessfulAsync(seqNum, move);
    }

    private static IEnumerable<OutboxMessage> GenerateRndMessages(int batchSize)
    {
        foreach (int _ in Enumerable.Range(0, batchSize))
        {
            yield return GenerateRndMessage();
        }
    }

    private static OutboxMessage GenerateRndMessage(string? partitionId = null)
    {
        var message = new
        {
            Id = Guid.NewGuid().ToString().ToLower(),
            Type = "test-message-type"
        };

        return new OutboxMessage(
            messageId: message.Id,
            messageType: message.Type,
            topic: nameof(PutBatchAsync_Inserts_Message_Rows),
            payload: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)))
        {
            PartitionId = partitionId ?? $"test-partition",
        };
    }

    private static void VerifyNew(OutboxMessage expectedMessage, IOutboxMessageRow row)
    {
        Assert.Equal(expectedMessage.MessageId, row.MessageId);
        Assert.Equal(expectedMessage.MessageType, row.MessageType);
        Assert.Equal(expectedMessage.PartitionId, row.PartitionId);
        Assert.Equal(expectedMessage.Topic, row.Topic);
        Assert.Equal(expectedMessage.Payload, row.Payload);

        Assert.Equal(0, row.RetryCount);
        Assert.Null(row.LastErrorAtUtc);
        Assert.Null(row.LockedAtUtc);
        Assert.Null(row.ProcessedAtUtc);
    }
}
