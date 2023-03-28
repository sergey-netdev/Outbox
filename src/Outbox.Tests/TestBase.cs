namespace Outbox.Tests;
using Outbox.Core;
using Outbox.Sql;
using System.Text;
using System.Text.Json;

public partial class TestBase
{
    // see ./docker/rabbitmq/etc/definitions.json
    public const string TopicA = "outbox.test.topicA";
    public const string TopicB = "outbox.test.topicB";
    public static readonly IReadOnlySet<string> Topics = new HashSet<string>(new[] { TopicA, TopicB });

    protected static IEnumerable<OutboxMessage> GenerateRndMessages(int batchSize, string? topic = null)
    {
        foreach (int _ in Enumerable.Range(0, batchSize))
        {
            yield return GenerateRndMessage(topic);
        }
    }

    private static string GetRndTopic() => Topics.OrderBy(x => Guid.NewGuid()).First();

    protected static OutboxMessage GenerateRndMessage(string? topic = null, string? partitionId = null)
    {
        var message = new
        {
            Id = Guid.NewGuid().ToString().ToLower(),
            Type = "test-message-type"
        };

        return new OutboxMessage(
            messageId: message.Id,
            messageType: message.Type,
            topic: topic ?? GetRndTopic(),
            payload: Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message)))
        {
            PartitionId = partitionId ?? $"test-partition",
        };
    }

    protected static IEnumerable<OutboxMessageRow> GenerateRndMessageRows(int batchSize, Action<OutboxMessageRow>? initAction = null)
    {
        foreach (int _ in Enumerable.Range(0, batchSize))
        {
            OutboxMessageRow result = GenerateRndMessageRow();
            initAction?.Invoke(result);
            yield return result;
        }
    }

    protected static OutboxMessageRow GenerateRndMessageRow(string? partitionId = null)
    {
        OutboxMessage message = GenerateRndMessage(partitionId); // for the sake of reuse
        OutboxMessageRow messageRow = new(message.MessageId, message.MessageType, message.Topic, message.Payload)
        {
            GeneratedAtUtc = DateTime.UtcNow,
            PartitionId = message.PartitionId
        };

        return messageRow;
    }

    protected static void VerifyNew(OutboxMessage expectedMessage, IOutboxMessageRow row)
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
