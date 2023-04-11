namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Outbox.Core;
using Outbox.Sql;
using System;

[Collection("Sequential")]
public partial class OutboxRepositoryTests : TestBase, IAsyncLifetime
{
    static readonly TimeSpan ClockSkewFix = TimeSpan.FromSeconds(1); // to compensate the difference between DateTime.UtcNow and Sql Server's GETUTCDATE() running in container
    const int QueryBatchSize = 10;
    const int UnlockBatchSize = 10;
    private readonly IHost _host;
    private readonly OutboxRepository _repository;
    private readonly OutboxRepositoryOptions options = new();

    public OutboxRepositoryTests()
    {
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddOutboxSqlRepository();
            })
            .Build();

        _repository = (OutboxRepository)_host.Services.GetRequiredService<IOutboxRepository>();
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

    [Theory]
    [InlineData(0)]
    [InlineData(UnlockBatchSize)]
    [InlineData(UnlockBatchSize + 2)]
    [InlineData(UnlockBatchSize - 2)]
    public async Task UnlockAsync_Removes_Expired_Locks(int totalLockedExpiredRows)
    {
        // setup
        DateTime now = DateTime.UtcNow;
        now = now.AddTicks(-now.Ticks % TimeSpan.TicksPerSecond); // round to the second cause Sql Server's datetime2 precision isn't enough
        TimeSpan lockDuration = TimeSpan.FromSeconds(120);
        _repository.Options.LockDuration = lockDuration;

        const int totalNonlockedRows = 5; // these must be unaffected
        OutboxMessageRow[] nonlockedMessageRows = GenerateRndMessageRows(totalNonlockedRows).ToArray();
        HashSet<string> nonlockedMessageRowKeys = (await _repository.InsertAsync(nonlockedMessageRows)).Keys.ToHashSet();

        DateTime lockedExpiredAtUtc = now - lockDuration - ClockSkewFix; // expired
        OutboxMessageRow[] lockedExpiredMessageRows = GenerateRndMessageRows(totalLockedExpiredRows, x => x.LockedAtUtc = lockedExpiredAtUtc).ToArray();
        HashSet<string> lockedExpiredMessageRowKeys = (await _repository.InsertAsync(lockedExpiredMessageRows)).Keys.ToHashSet();

        const int totalLockedActiveRows = 8; // these must be unaffected
        DateTime lockedActiveAtUtc = now; // non expired
        OutboxMessageRow[] lockedActiveMessageRows = GenerateRndMessageRows(totalLockedActiveRows, x => x.LockedAtUtc = lockedActiveAtUtc).ToArray();
        HashSet<string> lockedActiveMessageRowKeys = (await _repository.InsertAsync(lockedActiveMessageRows)).Keys.ToHashSet();

        // act
        int messageRowsUnlocked = await _repository.UnlockAsync(UnlockBatchSize);

        // verify
        Assert.Equal(Math.Min(UnlockBatchSize, totalLockedExpiredRows), messageRowsUnlocked);

        IReadOnlyCollection<IOutboxMessageRow> allMessageRows = await _repository.SelectAllAsync();
        HashSet<string> nonlockedIds = allMessageRows.Where(x => !x.LockedAtUtc.HasValue).Select(x => x.MessageId).ToHashSet();
        HashSet<string> lockedExpiredIds = allMessageRows.Where(x => x.LockedAtUtc == lockedExpiredAtUtc).Select(x => x.MessageId).ToHashSet();
        HashSet<string> lockedActiveIds = allMessageRows.Where(x => x.LockedAtUtc == lockedActiveAtUtc).Select(x => x.MessageId).ToHashSet();

        int expectedExpiredLockedRowsLeft = totalLockedExpiredRows - Math.Min(totalLockedExpiredRows, UnlockBatchSize);
        Assert.Equal(expectedExpiredLockedRowsLeft, lockedExpiredIds.Count);

        Assert.Equal(nonlockedIds , nonlockedMessageRowKeys.Concat(lockedExpiredMessageRowKeys.Take(UnlockBatchSize)));
        Assert.Equal(lockedActiveIds, lockedActiveMessageRowKeys); // non expired locked rows must be unaffected
        Assert.True(lockedExpiredIds.IsSubsetOf(lockedExpiredMessageRowKeys));
    }

    [Fact]
    public async Task PutBatchAsync_Inserts_Message_Rows()
    {
        // setup
        IReadOnlyDictionary<string, OutboxMessage> batch = GenerateRndMessages(QueryBatchSize).ToDictionary(x => x.MessageId, x => x);

        // act
        IReadOnlyDictionary<string, long> keys = await _repository.PutBatchAsync(batch.Values.ToArray());

        // verify
        Assert.NotNull(keys);
        Assert.Equal(QueryBatchSize, keys.Count);
        Assert.Equal(QueryBatchSize, keys.Values.Distinct().Count());
        Assert.Equal(batch.Select(x => x.Key), keys.Keys);
        foreach (KeyValuePair<string, long> key in keys)
        {
            IOutboxMessageRow? row = await _repository.SelectAsync(seqNum: key.Value);
            Assert.NotNull(row);

            OutboxMessage expectedMessage = batch[key.Key];
            VerifyNew(expectedMessage, row);
        }
    }

    [Fact]
    public async Task UpdateMessageAsSuccessfulAsync_Deletes_A_Message()
    {
        // setup
        OutboxMessage message = GenerateRndMessage();
        long seqNum = (await _repository.PutBatchAsync(new[] { message })).Single().Value;

        IOutboxMessageRow? messageRow = await _repository.SelectAsync(seqNum);
        Assert.NotNull(messageRow);
        Assert.Null(messageRow.ProcessedAtUtc);
        Assert.Null(await _repository.SelectProcessedAsync(seqNum));

        // act
        await _repository.UpdateMessageAsSuccessfulAsync(seqNum, move: false);

        // verify
        Assert.Null(await _repository.SelectAsync(seqNum));
        Assert.Null(await _repository.SelectProcessedAsync(seqNum));
    }

    [Fact]
    public async Task UpdateMessageAsSuccessfulAsync_Moves_A_Message()
    {
        // setup
        OutboxMessage message = GenerateRndMessage();
        long seqNum = (await _repository.PutBatchAsync(new[] { message })).Single().Value;

        IOutboxMessageRow? messageRow = await _repository.SelectAsync(seqNum);
        Assert.NotNull(messageRow);
        Assert.Null(messageRow.ProcessedAtUtc);
        Assert.Null(await _repository.SelectProcessedAsync(seqNum));

        // act
        await _repository.UpdateMessageAsSuccessfulAsync(seqNum, move: true);

        // verify
        Assert.Null(await _repository.SelectAsync(seqNum));
        IOutboxMessageRow? processedMessageRow = await _repository.SelectProcessedAsync(seqNum);
        Assert.NotNull(processedMessageRow);
        Assert.NotNull(processedMessageRow.ProcessedAtUtc);
    }

    [Fact]
    public async Task UpdateMessageAsUnsuccessfulAsync_Increases_RetryCount()
    {
        // setup
        OutboxMessage message = GenerateRndMessage();
        long seqNum = (await _repository.PutBatchAsync(new[] { message })).Single().Value;

        IOutboxMessageRow? messageRow = await _repository.SelectAsync(seqNum);
        Assert.NotNull(messageRow);
        Assert.Equal(0, messageRow.RetryCount);
        Assert.Null(messageRow.ProcessedAtUtc);
        Assert.Null(messageRow.LastErrorAtUtc);

        // act #1
        await _repository.UpdateMessageAsUnsuccessfulAsync(seqNum);

        // verify
        IOutboxMessageRow? messageRow1 = await _repository.SelectAsync(seqNum);
        Assert.NotNull(messageRow1);
        Assert.Equal(1, messageRow1.RetryCount);
        Assert.NotNull(messageRow1.LastErrorAtUtc);

        // act #2
        await _repository.UpdateMessageAsUnsuccessfulAsync(seqNum);

        // verify
        IOutboxMessageRow? messageRow2 = await _repository.SelectAsync(seqNum);
        Assert.NotNull(messageRow2);
        Assert.Equal(2, messageRow2.RetryCount);
        Assert.NotNull(messageRow2.LastErrorAtUtc);
        Assert.True(messageRow1.LastErrorAtUtc! < messageRow2.LastErrorAtUtc!);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(4)]
    public async Task UpdateMessageAsUnsuccessfulAsync_Moves_A_Message_When_MaxRetryCount_Is_Reached(byte maxRetryCount)
    {
        // setup
        _repository.Options.MaxRetryCount = maxRetryCount; // override the setting
        OutboxMessage message = GenerateRndMessage();
        long seqNum = (await _repository.PutBatchAsync(new[] { message })).Single().Value;

        Assert.Null(await _repository.SelectProcessedAsync(seqNum)); // nothing in OutboxProcessed
        IOutboxMessageRow? messageRow = await _repository.SelectAsync(seqNum);
        Assert.NotNull(messageRow);
        Assert.Null(messageRow.LastErrorAtUtc);
        Assert.Null(messageRow.ProcessedAtUtc);
        Assert.Equal(0, messageRow.RetryCount);

        // act
        byte i = maxRetryCount;
        do
        {
            await _repository.UpdateMessageAsUnsuccessfulAsync(seqNum);
        }
        while (i-- > 0);

        // verify
        Assert.Null(await _repository.SelectAsync(seqNum)); // nothing in Outbox
        messageRow = await _repository.SelectProcessedAsync(seqNum);
        Assert.NotNull(messageRow);
        Assert.NotNull(messageRow.LastErrorAtUtc);
        Assert.Null(messageRow.ProcessedAtUtc);
        Assert.Equal(maxRetryCount + 1, messageRow.RetryCount);
    }
}
