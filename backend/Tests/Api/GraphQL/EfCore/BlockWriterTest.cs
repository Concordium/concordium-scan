using System.Collections.Generic;
using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class BlockWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly BlockWriter _target;
    private readonly BlockInfoBuilder _blockInfoBuilder = new();
    private readonly BlockSummaryBuilder _blockSummaryBuilder = new();
    private readonly RewardStatusBuilder _rewardStatusBuilder = new();
    private readonly ImportState _importState = new();

    public BlockWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new BlockWriter(_dbContextFactory);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
        connection.Execute("TRUNCATE TABLE graphql_finalization_rewards");
        connection.Execute("TRUNCATE TABLE graphql_baking_rewards");
        connection.Execute("TRUNCATE TABLE graphql_finalization_summary_finalizers");
    }
    
    [Fact]
    public async Task BasicBlockInformation_AllValuesNonNull()
    {
        _blockInfoBuilder
            .WithBlockHash(new BlockHash("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc"))
            .WithBlockHeight(42)
            .WithBlockSlotTime(new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero))
            .WithBlockBaker(150)
            .WithFinalized(true)
            .WithTransactionCount(221);

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
    public async Task SpecialEvents_Mint_Exists()
    {
        _blockSummaryBuilder
            .WithSpecialEvents(new MintSpecialEventBuilder()
                .WithBakingReward(CcdAmount.FromMicroCcd(371021))
                .WithFinalizationReward(CcdAmount.FromMicroCcd(4577291))
                .WithPlatformDevelopmentCharge(CcdAmount.FromMicroCcd(2890562))
                .WithFoundationAccount(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"))
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.Should().NotBeNull();
        block.SpecialEvents.Mint.Should().NotBeNull();
        block.SpecialEvents.Mint!.BakingReward.Should().Be(371021);
        block.SpecialEvents.Mint.FinalizationReward.Should().Be(4577291);
        block.SpecialEvents.Mint.PlatformDevelopmentCharge.Should().Be(2890562);
        block.SpecialEvents.Mint.FoundationAccount.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task SpecialEvents_Mint_DoesNotExist()
    {
        _blockSummaryBuilder
            .WithSpecialEvents();
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.Mint.Should().BeNull();
    }

    [Fact]
    public async Task SpecialEvents_FinalizationRewards_Exist()
    {
        _blockSummaryBuilder
            .WithSpecialEvents(new FinalizationRewardsSpecialEventBuilder()
                .WithRemainder(CcdAmount.FromMicroCcd(371021))
                .WithFinalizationRewards(new()
                {
                    Amount = CcdAmount.FromMicroCcd(55511115),
                    Address = new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi")
                }, new()
                {
                    Amount = CcdAmount.FromMicroCcd(91425373),
                    Address = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")
                })
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.Owner.Should().BeSameAs(block);
        block.SpecialEvents.FinalizationRewards.Should().NotBeNull();
        block.SpecialEvents.FinalizationRewards!.Owner.Should().BeSameAs(block.SpecialEvents);
        block.SpecialEvents.FinalizationRewards.Remainder.Should().Be(371021);
        
        var result = dbContext.FinalizationRewards.ToArray();
        result.Length.Should().Be(2);
        result[0].BlockId.Should().Be(block.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Amount.Should().Be(55511115);
        result[0].Entity.Address.Should().Be("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi");
        result[1].BlockId.Should().Be(block.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Amount.Should().Be(91425373);
        result[1].Entity.Address.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }
    
    [Fact]
    public async Task SpecialEvents_FinalizationRewards_DoesNotExist()
    {
        _blockSummaryBuilder
            .WithSpecialEvents();
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.FinalizationRewards.Should().BeNull();
        
        var result = dbContext.FinalizationRewards.ToArray();
        result.Length.Should().Be(0);
    }

    [Fact]
    public async Task SpecialEvents_BlockRewards_Exists()
    {
        _blockSummaryBuilder
            .WithSpecialEvents(new BlockRewardSpecialEventBuilder()
                .WithBakerReward(CcdAmount.FromMicroCcd(5111884))
                .WithFoundationCharge(CcdAmount.FromMicroCcd(4884))
                .WithTransactionFees(CcdAmount.FromMicroCcd(8888))
                .WithNewGasAccount(CcdAmount.FromMicroCcd(455))
                .WithOldGasAccount(CcdAmount.FromMicroCcd(22))
                .WithBaker(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"))
                .WithFoundationAccount(new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi"))
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.Should().NotBeNull();
        block.SpecialEvents.BlockRewards.Should().NotBeNull();
        block.SpecialEvents.BlockRewards!.BakerReward.Should().Be(5111884);
        block.SpecialEvents.BlockRewards.FoundationCharge.Should().Be(4884);
        block.SpecialEvents.BlockRewards.TransactionFees.Should().Be(8888);
        block.SpecialEvents.BlockRewards.NewGasAccount.Should().Be(455);
        block.SpecialEvents.BlockRewards.OldGasAccount.Should().Be(22);
        block.SpecialEvents.BlockRewards.BakerAccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        block.SpecialEvents.BlockRewards.FoundationAccountAddress.Should().Be("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi");
    }

    [Fact]
    public async Task SpecialEvents_BlockRewards_DoesNotExist()
    {
        _blockSummaryBuilder
            .WithSpecialEvents();
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.BlockRewards.Should().BeNull();
    }

    [Fact]
    public async Task SpecialEvents_BakingRewards_Exist()
    {
        _blockSummaryBuilder
            .WithSpecialEvents(new BakingRewardsSpecialEventBuilder()
                .WithRemainder(CcdAmount.FromMicroCcd(371021))
                .WithBakerRewards(new()
                {
                    Amount = CcdAmount.FromMicroCcd(55511115),
                    Address = new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi")
                }, new()
                {
                    Amount = CcdAmount.FromMicroCcd(91425373),
                    Address = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")
                })
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.BakingRewards.Should().NotBeNull();
        block.SpecialEvents.BakingRewards!.Owner.Should().BeSameAs(block.SpecialEvents);
        block.SpecialEvents.BakingRewards.Remainder.Should().Be(371021);
        
        var result = dbContext.BakingRewards.ToArray();
        result.Length.Should().Be(2);
        result[0].BlockId.Should().Be(block.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Amount.Should().Be(55511115);
        result[0].Entity.Address.Should().Be("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi");
        result[1].BlockId.Should().Be(block.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Amount.Should().Be(91425373);
        result[1].Entity.Address.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }
    
    [Fact]
    public async Task SpecialEvents_BakingRewards_DoesNotExist()
    {
        _blockSummaryBuilder
            .WithSpecialEvents();
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.SpecialEvents.BakingRewards.Should().BeNull();
        
        var result = dbContext.BakingRewards.ToArray();
        result.Length.Should().Be(0);
    }

    [Fact]
    public async Task FinalizationSummary_NonNull()
    {
        _blockSummaryBuilder
            .WithFinalizationData(new FinalizationDataBuilder()
                .WithFinalizationBlockPointer(new BlockHash("86cb792754bc7bf2949378a8e1c9716a36877634a689d4e48198ceacb2e3591e"))
                .WithFinalizationIndex(42)
                .WithFinalizationDelay(11)
                .WithFinalizers(
                    new FinalizationSummaryPartyBuilder().WithBakerId(1).WithWeight(130).WithSigned(true).Build(),
                    new FinalizationSummaryPartyBuilder().WithBakerId(2).WithWeight(220).WithSigned(false).Build())
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.FinalizationSummary.Should().NotBeNull();
        block.FinalizationSummary!.Owner.Should().BeSameAs(block);
        block.FinalizationSummary.FinalizedBlockHash.Should().Be("86cb792754bc7bf2949378a8e1c9716a36877634a689d4e48198ceacb2e3591e");
        block.FinalizationSummary.FinalizationIndex.Should().Be(42);
        block.FinalizationSummary.FinalizationDelay.Should().Be(11);

        var finalizers = dbContext.FinalizationSummaryFinalizers.ToArray();
        finalizers.Length.Should().Be(2);
        finalizers[0].BlockId.Should().Be(block.Id);
        finalizers[0].Index.Should().Be(0);
        finalizers[0].Entity.BakerId.Should().Be(1);
        finalizers[0].Entity.Weight.Should().Be(130);
        finalizers[0].Entity.Signed.Should().BeTrue();
        finalizers[1].BlockId.Should().Be(block.Id);
        finalizers[1].Index.Should().Be(1);
        finalizers[1].Entity.BakerId.Should().Be(2);
        finalizers[1].Entity.Weight.Should().Be(220);
        finalizers[1].Entity.Signed.Should().BeFalse();
    }
    
    [Fact]
    public async Task FinalizationSummary_Null()
    {
        _blockSummaryBuilder
            .WithSpecialEvents();
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        block.FinalizationSummary.Should().BeNull();
        
        var result = dbContext.FinalizationSummaryFinalizers.ToArray();
        result.Length.Should().Be(0);
    }

    [Fact]
    public async Task BalanceStatistics_FromRewardStatus()
    {
        _rewardStatusBuilder
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
        block.BalanceStatistics.TotalEncryptedAmount.Should().Be(161);
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
    public async Task UpdateFinalizedBlocks_NoFinalizationProof()
    {
        var baseTime = new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero);

        var block = new BlockBuilder()
            .WithBlockSlotTime(baseTime)
            .WithFinalizationSummary(null)
            .Build();

        await _target.UpdateFinalizationTimeOnBlocksInFinalizationProof(block, _importState);
    }
    
    [Fact]
    public async Task UpdateFinalizedBlocks_FinalizationProofForSingleBlock()
    {
        _importState.MaxBlockHeightWithUpdatedFinalizationTime = 0;
        
        var baseTime = new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero);

        await AddBlock(new BlockBuilder().WithBlockHeight(10).WithBlockSlotTime(baseTime).WithBlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1").Build());

        var blockWithProof = new BlockBuilder()
            .WithBlockSlotTime(baseTime.AddSeconds(9))
            .WithFinalizationSummary(new FinalizationSummaryBuilder()
                .WithFinalizedBlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1")
                .Build())
            .Build();

        await _target.UpdateFinalizationTimeOnBlocksInFinalizationProof(blockWithProof, _importState);
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.Blocks.SingleAsync(x => x.BlockHeight == 10);
        result.BlockStatistics.FinalizationTime.Should().Be(9);

        _importState.MaxBlockHeightWithUpdatedFinalizationTime.Should().Be(10);
    }

    [Fact]
    public async Task UpdateFinalizedBlocks_FinalizationProofForMultipleBlocks()
    {
        var baseTime = new DateTimeOffset(2010, 10, 05, 12, 30, 20, 123, TimeSpan.Zero);

        await AddBlock(new BlockBuilder().WithBlockHeight(10).WithBlockSlotTime(baseTime.AddSeconds(10)).WithBlockHash("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1").Build());
        await AddBlock(new BlockBuilder().WithBlockHeight(11).WithBlockSlotTime(baseTime.AddSeconds(19)).WithBlockHash("01cc0746f74640292e2f1bcc5fd4a542678c88c7a840adfca365612278160845").Build());
        await AddBlock(new BlockBuilder().WithBlockHeight(12).WithBlockSlotTime(baseTime.AddSeconds(31)).WithBlockHash("9408d0d26faf8b4cc99722ab27b094b8a27b251d8133ae690ea92b68caa689a2").Build());

        _importState.MaxBlockHeightWithUpdatedFinalizationTime = 10;
        
        var blockWithProof = new BlockBuilder()
            .WithBlockSlotTime(baseTime.AddSeconds(40))
            .WithFinalizationSummary(new FinalizationSummaryBuilder()
                .WithFinalizedBlockHash("9408d0d26faf8b4cc99722ab27b094b8a27b251d8133ae690ea92b68caa689a2")
                .Build())
            .Build();

        await _target.UpdateFinalizationTimeOnBlocksInFinalizationProof(blockWithProof, _importState);
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.Blocks.ToArrayAsync();
        result.Should().ContainSingle(x => x.BlockHeight == 10).Which.BlockStatistics.FinalizationTime.Should().BeNull();
        result.Should().ContainSingle(x => x.BlockHeight == 11).Which.BlockStatistics.FinalizationTime.Should().Be(21);
        result.Should().ContainSingle(x => x.BlockHeight == 12).Which.BlockStatistics.FinalizationTime.Should().Be(9);

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
                insert into graphql_account_release_schedule (account_id, transaction_id, schedule_index, timestamp, amount)
                values (1, 1, 1, @Timestamp, @Amount)", schedules);
    }

    private async Task WriteData()
    {
        var blockInfo = _blockInfoBuilder.Build();
        var blockSummary = _blockSummaryBuilder.Build();
        var rewardStatus = _rewardStatusBuilder.Build();
        await _target.AddBlock(blockInfo, blockSummary, rewardStatus, _importState);
    }
}