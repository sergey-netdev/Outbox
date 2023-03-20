﻿namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Outbox.Core;
using Outbox.Sql;
using System.Text;
using System.Text.Json;

public class OutboxRepositoryTests
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

    [Fact]
    public async Task LockAndGetNextBatchAsync_Returns_Specified_Number_Of_Message_Rows()
    {
        IReadOnlyCollection<IOutboxMessageRow> batch = await _repository.LockAndGetNextBatchAsync(QueryBatchSize);
        Assert.NotNull(batch);
        Assert.Equal(QueryBatchSize, batch.Count);
        Assert.Equal(batch.Count, batch.Select(x => x.MessageId).Distinct().Count());

        foreach (IOutboxMessageRow m in batch)
        {
            Assert.Equal(0, m.RetryCount);
            //Assert.Null(m.PartitionId);
            Assert.NotNull(m.MessageId);
            Assert.NotNull(m.MessageType);
            Assert.True(DateTimeOffset.UtcNow > m.GeneratedAtUtc);
            Assert.Null(m.LastErrorAtUtc);
            Assert.Null(m.LockedAtUtc);
            Assert.Null(m.ProcessedAtUtc);
        }
    }

    [Fact]
    public async Task PutBatchAsync_Inserts_Message_Rows()
    {
        const int batchSize = 10;

        List<OutboxMessage> batch = GenerateRndMessages(batchSize);

        IReadOnlyDictionary<string, long> keys = await _repository.PutBatchAsync(batch);
        Assert.NotNull(keys);
        Assert.Equal(batchSize, keys.Count);
        Assert.Equal(batchSize, keys.Values.Distinct().Count());
        Assert.Equal(batch.Select(x => x.MessageId), keys.Keys);
    }

    private async Task SetupAsync()
    {
        await _repository.ClearAsync();
    }

    private List<OutboxMessage> GenerateRndMessages(int batchSize)
    {
        List<OutboxMessage> messages = Enumerable.Range(0, batchSize).Select(x =>
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
                PartitionId = $"partition-{x}",
            };
        })
        .ToList();

        return messages;
    }
}
