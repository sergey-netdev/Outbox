namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Outbox.Core;
using Outbox.Job;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Unicode;

public class OutboxRepositoryTests
{
    private readonly IOutboxRepository _repository;

    public OutboxRepositoryTests()
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .AddJsonFile("appsettings.json")
            .Build();

        OutboxOptions outboxOptions = new();
        configuration.GetSection(OutboxOptions.DefaultSectionName).Bind(outboxOptions);

        _repository = new OutboxRepository(outboxOptions);
    }

    [Fact]
    public async Task GetNextBatchAsync_Returns_Specified_Number_Of_Message_Rows()
    {
        IReadOnlyCollection<IOutboxMessage> batch = await _repository.GetNextBatchAsync();
        Assert.NotEmpty(batch);
    }

    [Fact]
    public async Task PutBatchAsync_Inserts_Message_Rows()
    {
        const int batchSize = 10;

        List<OutboxMessage> batch = Enumerable.Range(0, batchSize).Select(x =>
        {
            var message = new {
                Id = Guid.NewGuid().ToString().ToLower(),
                Type = "test-message-type"
            };

            return new OutboxMessage(
            messageId: message.Id,
            messageType: message.Type,
            topic: nameof(PutBatchAsync_Inserts_Message_Rows),
            partitionId: $"partition-{x}",
            payload: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)));
        })
        .ToList();

        IReadOnlyDictionary<string, long> keys = await _repository.PutBatchAsync(batch);
        Assert.NotNull(keys);
        Assert.Equal(batchSize, keys.Count);
        Assert.Equal(batchSize, keys.Values.Distinct().Count());
        Assert.Equal(batch.Select(x => x.MessageId), keys.Keys);
    }
}
