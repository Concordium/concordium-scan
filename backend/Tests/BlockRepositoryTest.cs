using System.Text.Json;
using Application.Persistence;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests;

[Collection("Postgres Collection")]
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
    public void Insert_WithMintSpecialEvent()
    {
        var blockInfo = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithSpecialEvents(new BakingRewardsSpecialEventBuilder().Build())
            .Build();
        
        _target.Insert(blockInfo, "{\"bar\": \"baz\"}", blockSummary);
    }

    [Fact]
    public void Insert_TransactionSummarySenderNull()
    {
        var blockInfo = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithSender(null)
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
                    .WithResult(new TransactionSuccessResult() { Events = Array.Empty<TransactionResultEvent>() })
                    .Build(),
                new TransactionSummaryBuilder()
                    .WithIndex(1)
                    .WithType(TransactionType.Get((AccountTransactionType?)null))
                    .WithResult(new TransactionRejectResult { Reason = new ModuleNotWf()})
                    .Build()
            }
        };

        _target.Insert(blockInfo, "{\"foo\": \"bar\"}", blockSummary);
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
        Assert.NotNull(result);
        Assert.Equal("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc", result!.AsString);
    }
}