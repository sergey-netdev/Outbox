﻿namespace Outbox.Tests;
using Microsoft.Extensions.Configuration;
using Outbox.Core;
using Outbox.Sql;
using System.Text;
using System.Text.Json;

[Collection("Sequential")]
public partial class OutboxRepositoryTests : IAsyncLifetime
{
    static readonly TimeSpan ClockSkewFix = TimeSpan.FromSeconds(1); // to compensate the difference between DateTime.UtcNow and Sql Server's GETUTCDATE() running in container
    const int QueryBatchSize = 10;
    const int UnlockBatchSize = 10;
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

    [Theory]
    [InlineData(0)]
    [InlineData(UnlockBatchSize)]
    [InlineData(UnlockBatchSize + 2)]
    [InlineData(UnlockBatchSize - 2)]
    public async Task UnlockAsync_Removes_Expired_Locks(int totalLockedRows)
    {
        // setup
        TimeSpan lockDuration = TimeSpan.FromSeconds(7);
        _repository.Options.LockDuration = lockDuration;

        const int totalNonlockedRows = 5;
        OutboxMessageRow[] nonlockedMessageRows = GenerateRndMessageRows(totalNonlockedRows).ToArray();
        IReadOnlyDictionary<string, long> nonlockedMessageRowKeys = await _repository.InsertAsync(nonlockedMessageRows);

        DateTime now = DateTime.UtcNow;
        DateTime lockedAtUtc = now - lockDuration - ClockSkewFix; // expired datetime
        OutboxMessageRow[] lockedMessageRows = GenerateRndMessageRows(totalLockedRows, x => x.LockedAtUtc = lockedAtUtc).ToArray();
        IReadOnlyDictionary<string, long> lockedMessageRowKeys = await _repository.InsertAsync(lockedMessageRows);

        // act
        int messageRowsUnlocked = await _repository.UnlockAsync(UnlockBatchSize);
        Assert.Equal(Math.Min(UnlockBatchSize, totalLockedRows), messageRowsUnlocked);

        // verify
        IReadOnlyCollection<IOutboxMessageRow> allMessageRows = await _repository.SelectAllAsync();
        HashSet<string> nonlockedIds = allMessageRows.Where(x => !x.LockedAtUtc.HasValue).Select(x => x.MessageId).ToHashSet();
        HashSet<string> lockedIds = allMessageRows.Where(x => x.LockedAtUtc.HasValue).Select(x => x.MessageId).ToHashSet();

        int expectedLockedRowsLeft = totalLockedRows - Math.Min(totalLockedRows, UnlockBatchSize);
        Assert.Equal(expectedLockedRowsLeft, lockedIds.Count);

        Assert.True(nonlockedIds.IsSupersetOf(nonlockedMessageRowKeys.Keys)); // originally nonlocked rows must stay intact
    }

    [Fact]
    public async Task UnlockAsync_Does_Not_Remove_NonExpired_Locks()
    {

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
