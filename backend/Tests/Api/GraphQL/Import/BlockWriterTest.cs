using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Import;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class BlockWriterTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly BlockWriter _target;
    private readonly BlockInfoBuilder _blockInfoBuilder = new();
    private readonly RewardOverviewV0Builder _rewardOverviewV0Builder = new();
    private readonly ImportState _importState = new ImportStateBuilder().Build();
    private readonly BakerUpdateResultsBuilder _bakerUpdateResultsBuilder = new();
    private readonly DelegationUpdateResultsBuilder _delegationUpdateResultsBuilder = new();
    private int _chainParametersId = 20;

    public BlockWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new BlockWriter(_dbContextFactory, new NullMetrics());

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
    }
    
    [Fact]
    public async Task BasicBlockInformation_AllValuesNonNull()
    {
        _blockInfoBuilder
            .WithBlockHash(BlockHash.From("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc"))
            .WithBlockHeight(42)
            .WithBlockSlotTime(new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero))
            .WithBlockBaker(new BakerId(new AccountIndex(150)))
            .WithFinalized(true)
            .WithTransactionCount(221);
        
        _chainParametersId = 42;

        _bakerUpdateResultsBuilder.WithTotalAmountStaked(10000);
        _delegationUpdateResultsBuilder.WithTotalAmountStaked(5000);
        
        await WriteData();

        var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Blocks.Single();
        
        result.Id.Should().BeGreaterThan(0);
        result.BlockHash.Should().Be("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc");
        result.BlockHeight.Should().Be(42);
        result.BlockSlotTime.Should().Be(new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero));
        result.BakerId.Should().Be(150);
        result.Finalized.Should().BeTrue();
        result.TransactionCount.Should().Be(221);
        result.ChainParametersId.Should().Be(42);
        result.BalanceStatistics.TotalAmountStaked.Should().Be(15000);
        result.BalanceStatistics.TotalAmountStakedByBakers.Should().Be(10000);
        result.BalanceStatistics.TotalAmountStakedByDelegation.Should().Be(5000);
    }
    
    [Fact]
    public async Task BasicBlockInformation_NullableValuesNull()
    {
        _blockInfoBuilder.WithBlockBaker(null);

        await WriteData();

        var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Blocks.Single();
        result.BakerId.Should().BeNull();
    }

    [Fact]
    public async Task BalanceStatistics_FromRewardStatus()
    {
        _rewardOverviewV0Builder
            .WithTotalAmount(CcdAmount.FromMicroCcd(421500))
            .WithTotalEncryptedAmount(CcdAmount.FromMicroCcd(161))
            .WithBakingRewardAccount(CcdAmount.FromMicroCcd(77551))
            .WithFinalizationRewardAccount(CcdAmount.FromMicroCcd(922438))
            .WithGasAccount(CcdAmount.FromMicroCcd(35882));
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.BalanceStatistics.Should().NotBeNull();
        block.BalanceStatistics.TotalAmount.Should().Be(421500);
        block.BalanceStatistics.TotalAmountEncrypted.Should().Be(161);
        block.BalanceStatistics.BakingRewardAccount.Should().Be(77551);
        block.BalanceStatistics.FinalizationRewardAccount.Should().Be(922438);
        block.BalanceStatistics.GasAccount.Should().Be(35882);
    }

    [Fact]
    public async Task UpdateTotalAmountLockedInReleaseSchedules_NoReleaseSchedulesExist()
    {
        // Create and get a block
        await WriteData();
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();

        await SetReleaseSchedule(Array.Empty<object>());

        // act!
        await _target.UpdateTotalAmountLockedInReleaseSchedules(block);

        // assert!
        var writtenResult = dbContext.Blocks.Single();
        writtenResult.BalanceStatistics.TotalAmountLockedInReleaseSchedules.Should().Be(0);
        block.BalanceStatistics.TotalAmountLockedInReleaseSchedules.Should().Be(0);
    }
    
    [Fact]
    public async Task UpdateTotalAmountLockedInReleaseSchedules_ReleaseSchedulesExist()
    {
        await WriteData();
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.AsNoTracking().Single();

        var schedules = new object[]
        {
            new { Timestamp = block.BlockSlotTime.AddHours(-1), Amount = 10 },  // not expected included 
            new { Timestamp = block.BlockSlotTime.AddHours(0), Amount = 100 },  // not expected included
            new { Timestamp = block.BlockSlotTime.AddHours(1), Amount = 1000 }, // expected included
            new { Timestamp = block.BlockSlotTime.AddHours(2), Amount = 10000 } // expected included
        };
        await SetReleaseSchedule(schedules);

        await _target.UpdateTotalAmountLockedInReleaseSchedules(block);

        var writtenResult = dbContext.Blocks.AsNoTracking().Single();
        writtenResult.BalanceStatistics.TotalAmountLockedInReleaseSchedules.Should().Be(11000);
        block.BalanceStatistics.TotalAmountLockedInReleaseSchedules.Should().Be(11000);
    }

    [Theory]
    [InlineData(2200, 2.2)]
    [InlineData(2250, 2.2)] // rounding to nearest even, in this case down
    [InlineData(2350, 2.4)] // rounding to nearest even, in this case up
    public async Task BlockStatistics_BlockTime_NotGenesisBlock_CacheInitialized(int blockSlotTimeAdjustment, double expectedResult)
    {
        var baseTime = new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero);
        _importState.LastBlockSlotTime = baseTime;

        _blockInfoBuilder
            .WithBlockHeight(1)
            .WithBlockSlotTime(baseTime.AddMilliseconds(blockSlotTimeAdjustment));
        
        await WriteData();

        var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Blocks.Single();
        result.BlockStatistics.Should().NotBeNull();
        result.BlockStatistics.BlockTime.Should().Be(expectedResult);
    }

    [Fact]
    public async Task UpdateFinalizedBlocks_FinalizationProofForSingleBlock()
    {
        _importState.MaxBlockHeightWithUpdatedFinalizationTime = 0;
        const string lastBlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";

        var baseTime = new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero);
        const double secondsToAdd = 9.57;
        var block = new BlockBuilder()
            .WithBlockHeight(10)
            .WithBlockSlotTime(baseTime)
            .WithBlockHash(lastBlockHash)
            .Build();

        await AddBlock(block);

        var blockInfo = new BlockInfoBuilder()
            .WithBlockSlotTime(baseTime.AddSeconds(secondsToAdd))
            .WithBlockLastFinalized(BlockHash.From(lastBlockHash))
            .Build();

        await _target.UpdateFinalizationTimeOnBlocksInFinalizationProof(blockInfo, _importState);
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.Blocks.SingleAsync(x => x.BlockHeight == 10);
        result.BlockStatistics.FinalizationTime.Should().Be(Math.Round(secondsToAdd, 1));

        _importState.MaxBlockHeightWithUpdatedFinalizationTime.Should().Be(10);
    }

    [Fact]
    public async Task UpdateFinalizedBlocks_FinalizationProofForMultipleBlocks()
    {
        var baseTime = new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero);
        const string lastBlockHash = "9408d0d26faf8b4cc99722ab27b094b8a27b251d8133ae690ea92b68caa689a2";
        const double secondsToAdd = 40.52;
        
        await AddBlock(new BlockBuilder().WithBlockHeight(10).WithBlockSlotTime(baseTime.AddSeconds(10)).WithBlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1").Build());
        await AddBlock(new BlockBuilder().WithBlockHeight(11).WithBlockSlotTime(baseTime.AddSeconds(19)).WithBlockHash("01cc0746f74640292e2f1bcc5fd4a542678c88c7a840adfca365612278160845").Build());
        await AddBlock(new BlockBuilder().WithBlockHeight(12).WithBlockSlotTime(baseTime.AddSeconds(31)).WithBlockHash("9408d0d26faf8b4cc99722ab27b094b8a27b251d8133ae690ea92b68caa689a2").Build());

        _importState.MaxBlockHeightWithUpdatedFinalizationTime = 10;
        
        var blockInfo = new BlockInfoBuilder()
            .WithBlockSlotTime(baseTime.AddSeconds(secondsToAdd))
            .WithBlockLastFinalized(BlockHash.From(lastBlockHash))
            .Build();

        await _target.UpdateFinalizationTimeOnBlocksInFinalizationProof(blockInfo, _importState);
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.Blocks.ToArrayAsync();
        result.Should().ContainSingle(x => x.BlockHeight == 10).Which.BlockStatistics.FinalizationTime.Should().BeNull();
        result.Should().ContainSingle(x => x.BlockHeight == 11).Which.BlockStatistics.FinalizationTime.Should().Be(21.5);
        result.Should().ContainSingle(x => x.BlockHeight == 12).Which.BlockStatistics.FinalizationTime.Should().Be(9.5);

        _importState.MaxBlockHeightWithUpdatedFinalizationTime.Should().Be(12);
    }

    private async Task AddBlock(Block finalizedBlock)
    {
        var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Blocks.Add(finalizedBlock);
        await dbContext.SaveChangesAsync();
    }

    private async Task SetReleaseSchedule(object[] schedules)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var conn = dbContext.Database.GetDbConnection();
        await conn.ExecuteAsync("TRUNCATE TABLE graphql_account_release_schedule");
        
        // account_id, transaction_id and schedule_index values do not matter!
        await conn.ExecuteAsync(@"
                insert into graphql_account_release_schedule (account_id, transaction_id, schedule_index, timestamp, amount, from_account_id)
                values (1, 1, 1, @Timestamp, @Amount, 2)", schedules);
    }

    private async Task WriteData()
    {
        var blockInfo = _blockInfoBuilder.Build();
        var rewardStatus = _rewardOverviewV0Builder.Build();
        var bakerUpdateResults = _bakerUpdateResultsBuilder.Build();
        var delegationUpdateResults = _delegationUpdateResultsBuilder.Build();
        await _target.AddBlock(blockInfo, rewardStatus, _chainParametersId, bakerUpdateResults, delegationUpdateResults, _importState, Array.Empty<ulong>());
    }
}
