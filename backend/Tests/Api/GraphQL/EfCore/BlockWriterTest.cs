using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class BlockWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly BlockWriter _target;
    private readonly BlockInfoBuilder _blockInfoBuilder = new();
    private readonly BlockSummaryBuilder _blockSummaryBuilder = new();
    private readonly RewardStatusBuilder _rewardStatusBuilder = new();

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
    
    private async Task WriteData()
    {
        var blockInfo = _blockInfoBuilder.Build();
        var blockSummary = _blockSummaryBuilder.Build();
        var rewardStatus = _rewardStatusBuilder.Build();
        await _target.AddBlock(blockInfo, blockSummary, rewardStatus);
    }
}