using System.Text.Json;
using Application.Persistence;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests;

public class BlockRepositoryTest : IClassFixture<DatabaseFixture>
{
    private readonly DatabaseFixture _dbFixture;
    private readonly BlockRepository _target;

    public BlockRepositoryTest(DatabaseFixture dbFixture)
    {
        _dbFixture = dbFixture;
        _target = new BlockRepository(_dbFixture.DatabaseSettings);
        
        using var connection = _dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE block");
        connection.Execute("TRUNCATE TABLE finalized_block");
        connection.Execute("TRUNCATE TABLE transaction_summary");
    }

    [Fact]
    public void Insert()
    {
        var blockInfo = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder().Build();
        _target.Insert(blockInfo, "{\"foo\": \"bar\"}", blockSummary);
    }

    [Fact]
    public void Insert_TransactionSummarySenderNull()
    {
        var blockInfo = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .withSender(null)
                .Build())
            .Build();
        
        _target.Insert(blockInfo, "{\"foo\": \"bar\"}", blockSummary);
    }

    [Fact]
    public void GetMaxBlockHeight_NoBlocksExist()
    {
        var result = _target.GetMaxBlockHeight();
        Assert.False(result.HasValue);
    }
    
    [Fact]
    public void GetMaxBlockHeight_BlocksExist()
    {
        _target.Insert(new BlockInfoBuilder().WithBlockHeight(1).Build(), "{\"foo\": \"bar\"}", new BlockSummaryBuilder().Build());
        _target.Insert(new BlockInfoBuilder().WithBlockHeight(3).Build(), "{\"foo\": \"bar\"}", new BlockSummaryBuilder().Build());
        _target.Insert(new BlockInfoBuilder().WithBlockHeight(2).Build(), "{\"foo\": \"bar\"}", new BlockSummaryBuilder().Build());

        var result = _target.GetMaxBlockHeight();
        Assert.Equal(3, result);
    }
    
    [Fact]
    public void Insert_new()
    {
        var blockInfo = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummary
        {
            TransactionSummaries = new[]
            {
                new TransactionSummaryBuilder()
                    .WithIndex(0)
                    .WithResult(new TransactionSuccessResult() { Events = JsonDocument.Parse("[]").RootElement })
                    .Build(),
                new TransactionSummaryBuilder()
                    .WithIndex(1)
                    .WithType(TransactionType.Get((AccountTransactionType?)null))
                    .WithResult(new TransactionRejectResult() { Tag = "FooBarBas!"})
                    .Build()
            }
        };

        _target.Insert(blockInfo, "{\"foo\": \"bar\"}", blockSummary);
    }

    [Fact]
    public void FindTransactionSummaries_EmptyTable()
    {
        var startTime = new DateTimeOffset(2020, 01, 02, 10, 00, 00, TimeSpan.Zero);
        var endTime = new DateTimeOffset(2020, 01, 02, 10, 10, 00, TimeSpan.Zero);
        var result = _target.FindTransactionSummaries(startTime, endTime, TransactionType.Get(AccountTransactionType.SimpleTransfer));
        Assert.Empty(result);
    }
    
    [Theory]
    [InlineData(-60, -30, 0)]  
    [InlineData(150, 180, 0)]  
    [InlineData(90, 110, 0)]   
    [InlineData(119, 121, 1)]  
    [InlineData(120, 120, 1)]  
    [InlineData(60, 120, 2)]   
    [InlineData(0, 120, 3)]    
    public void FindTransactionSummaries_SingleRow(int startTimeOffset, int endTimeOffset, int expectedNumberOfResults)
    {
        var blockSlotTime = new DateTimeOffset(2020, 01, 02, 10, 00, 00, TimeSpan.Zero);

        for (var i = 0; i < 3; i++)
        {
            var blockInfo = new BlockInfoBuilder()
                .WithBlockHeight(i+1)
                .WithBlockSlotTime(blockSlotTime.AddHours(i))
                .Build();

            var blockSummary = new BlockSummaryBuilder()
                .WithTransactionSummaries(
                    new TransactionSummaryBuilder()
                        .WithType(TransactionType.Get(AccountTransactionType.SimpleTransfer))
                        .Build())
                .Build();

            _target.Insert(blockInfo, "{}", blockSummary);
        }

        var startTime = blockSlotTime.AddMinutes(startTimeOffset);
        var endTime = blockSlotTime.AddMinutes(endTimeOffset);
        
        var result = _target.FindTransactionSummaries(startTime, endTime, TransactionType.Get(AccountTransactionType.SimpleTransfer));
        Assert.Equal(expectedNumberOfResults, result.Length);
    }

    [Fact]
    public void GetGenesisBlockHash_EmptyDatabase()
    {
        var result = _target.GetGenesisBlockHash();
        Assert.Null(result);
    }
    
    [Fact]
    public void GetGenesisBlockHash_NonEmptyDatabase()
    {
        var blockInfo = new BlockInfoBuilder()
            .WithBlockHeight(0)
            .WithBlockHash(new BlockHash("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc"))
            .Build();
        var blockSummary = new BlockSummaryBuilder().Build();
        
        _target.Insert(blockInfo, "{\"foo\": \"bar\"}", blockSummary);

        var result = _target.GetGenesisBlockHash();
        Assert.Equal("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc", result.Value.AsString);
    }
}