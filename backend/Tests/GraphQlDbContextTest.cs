using Application.Api.GraphQL;
using Application.Api.GraphQL.EfCore;
using Application.Persistence;
using ConcordiumSdk.Types;
using Dapper;
using Microsoft.Extensions.Logging.Abstractions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;

namespace Tests;

[Collection("Postgres Collection")]
public class GraphQlDbContextTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContext _target;
    private readonly BlockRepository _writeRepository;

    public GraphQlDbContextTest(DatabaseFixture dbFixture)
    {
        _writeRepository = new BlockRepository(dbFixture.DatabaseSettings);
        _target = new GraphQlDbContext(dbFixture.DatabaseSettings, new NullLoggerFactory());
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE block");
        connection.Execute("TRUNCATE TABLE transaction_summary");
        connection.Execute("TRUNCATE TABLE finalization_reward");
        connection.Execute("TRUNCATE TABLE baking_reward");
        connection.Execute("TRUNCATE TABLE finalization_data_finalizers");
    }

    [Fact]
    public void Blocks_SpecialEvents_None()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithSpecialEvents()
            .Build();
        
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.NotNull(single.SpecialEvents);
        Assert.Null(single.SpecialEvents.Mint);
        Assert.Null(single.SpecialEvents.FinalizationRewards);
        Assert.Null(single.SpecialEvents.BlockRewards);
        Assert.Null(single.SpecialEvents.BakingRewards);
    }
    
    [Fact]
    public void Blocks_SpecialEvents_OnlyMint()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithSpecialEvents(new MintSpecialEventBuilder()
                .WithBakingReward(CcdAmount.FromMicroCcd(371021))
                .WithFinalizationReward(CcdAmount.FromMicroCcd(4577291))
                .WithPlatformDevelopmentCharge(CcdAmount.FromMicroCcd(2890562))
                .WithFoundationAccount(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"))
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.NotNull(single.SpecialEvents);
        Assert.NotNull(single.SpecialEvents.Mint);
        Assert.Equal(371021, single.SpecialEvents.Mint!.BakingReward);
        Assert.Equal(4577291, single.SpecialEvents.Mint.FinalizationReward);
        Assert.Equal(2890562, single.SpecialEvents.Mint.PlatformDevelopmentCharge);
        Assert.Equal("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd", single.SpecialEvents.Mint.FoundationAccount);
    }
    
    [Fact]
    public void Blocks_SpecialEvents_OnlyFinalizationReward()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
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
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.NotNull(single.SpecialEvents);
        Assert.NotNull(single.SpecialEvents.FinalizationRewards);
        Assert.Equal(371021, single.SpecialEvents.FinalizationRewards!.Remainder);
        Assert.Equal(2, single.SpecialEvents.FinalizationRewards.Rewards.Count());
        Assert.Equal(55511115, single.SpecialEvents.FinalizationRewards.Rewards.ElementAt(0).Amount);
        Assert.Equal("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi", single.SpecialEvents.FinalizationRewards.Rewards.ElementAt(0).Address);
        Assert.Equal(91425373, single.SpecialEvents.FinalizationRewards.Rewards.ElementAt(1).Amount);
        Assert.Equal("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd", single.SpecialEvents.FinalizationRewards.Rewards.ElementAt(1).Address);
    }
    
    [Fact]
    public void Blocks_SpecialEvents_OnlyBlockRewards()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithSpecialEvents(new BlockRewardSpecialEventBuilder()
                .WithBakerReward(CcdAmount.FromMicroCcd(5111884))
                .WithFoundationCharge(CcdAmount.FromMicroCcd(4884))
                .WithTransactionFees(CcdAmount.FromMicroCcd(8888))
                .WithNewGasAccount(CcdAmount.FromMicroCcd(455))
                .WithOldGasAccount(CcdAmount.FromMicroCcd(22))
                .WithBaker(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"))
                .WithFoundationAccount(new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi"))
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.NotNull(single.SpecialEvents);
        Assert.NotNull(single.SpecialEvents.BlockRewards);
        Assert.Equal(5111884, single.SpecialEvents.BlockRewards!.BakerReward);
        Assert.Equal(4884, single.SpecialEvents.BlockRewards.FoundationCharge);
        Assert.Equal(8888, single.SpecialEvents.BlockRewards.TransactionFees);
        Assert.Equal(455, single.SpecialEvents.BlockRewards.NewGasAccount);
        Assert.Equal(22, single.SpecialEvents.BlockRewards.OldGasAccount);
        Assert.Equal("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd", single.SpecialEvents.BlockRewards.BakerAccountAddress);
        Assert.Equal("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi", single.SpecialEvents.BlockRewards.FoundationAccountAddress);
    }
    
    [Fact]
    public void Blocks_SpecialEvents_OnlyBakingRewards()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
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
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.NotNull(single.SpecialEvents);
        Assert.NotNull(single.SpecialEvents.BakingRewards);
        Assert.Equal(371021, single.SpecialEvents.BakingRewards!.Remainder);
        Assert.Equal(2, single.SpecialEvents.BakingRewards.Rewards.Count());
        Assert.Equal(55511115, single.SpecialEvents.BakingRewards.Rewards.ElementAt(0).Amount);
        Assert.Equal("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi", single.SpecialEvents.BakingRewards.Rewards.ElementAt(0).Address);
        Assert.Equal(91425373, single.SpecialEvents.BakingRewards.Rewards.ElementAt(1).Amount);
        Assert.Equal("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd", single.SpecialEvents.BakingRewards.Rewards.ElementAt(1).Address);
    }

    [Fact]
    public void Blocks_FinalizationSummary_Null()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithFinalizationData(null)
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.Null(single.FinalizationSummary);
    }
    
    [Fact]
    public void Blocks_FinalizationSummary_HasData()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithFinalizationData(new FinalizationDataBuilder()
                .WithFinalizationBlockPointer(new BlockHash("86cb792754bc7bf2949378a8e1c9716a36877634a689d4e48198ceacb2e3591e"))
                .WithFinalizationIndex(42)
                .WithFinalizationDelay(11)
                .WithFinalizers(
                    new FinalizationSummaryPartyBuilder().WithBakerId(1).WithWeight(130).WithSigned(true).Build(),
                    new FinalizationSummaryPartyBuilder().WithBakerId(2).WithWeight(220).WithSigned(false).Build())
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var blocks = _target.Blocks;
        
        var single = Assert.Single(blocks);
        Assert.NotNull(single.FinalizationSummary);
        Assert.Equal("86cb792754bc7bf2949378a8e1c9716a36877634a689d4e48198ceacb2e3591e", single.FinalizationSummary!.FinalizedBlockHash);
        Assert.Equal(42, single.FinalizationSummary.FinalizationIndex);
        Assert.Equal(11, single.FinalizationSummary.FinalizationDelay);
        Assert.Equal(2, single.FinalizationSummary.Finalizers.Count());
        Assert.Equal(1, single.FinalizationSummary.Finalizers.ElementAt(0).BakerId);
        Assert.Equal(130, single.FinalizationSummary.Finalizers.ElementAt(0).Weight);
        Assert.True(single.FinalizationSummary.Finalizers.ElementAt(0).Signed);
        Assert.Equal(2, single.FinalizationSummary.Finalizers.ElementAt(1).BakerId);
        Assert.Equal(220, single.FinalizationSummary.Finalizers.ElementAt(1).Weight);
        Assert.False(single.FinalizationSummary.Finalizers.ElementAt(1).Signed);
    }

    [Fact]
    public void Transactions_None()
    {
        var result = _target.Transactions;
        Assert.Empty(result);
    }
    
    [Fact]
    public void Transactions_Single()
    {
        var block = new BlockInfoBuilder()
            .WithBlockHeight(42)
            .WithBlockHash(new("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc"))
            .Build();
        
        var blockSummary = new BlockSummaryBuilder()
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithIndex(0)
                .WithSender(new("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"))
                .WithTransactionHash(new("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b"))
                .WithCost(CcdAmount.FromMicroCcd(45872))
                .WithEnergyCost(399)
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var result = _target.Transactions;
        var single = Assert.Single(result);
        Assert.NotNull(single);
        Assert.Equal(0, single.TransactionIndex);
        Assert.Equal(42, single.BlockHeight);
        Assert.Equal("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc", single.BlockHash);
        Assert.Equal("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd", single.SenderAccountAddress);
        Assert.Equal("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b", single.TransactionHash);
        Assert.Equal(45872, single.CcdCost);
        Assert.Equal(399, single.EnergyCost);
        var accountTransaction = Assert.IsType<AccountTransaction>(single.TransactionType);
        Assert.Equal(AccountTransactionType.SimpleTransfer, accountTransaction.AccountTransactionType);
    }
    
    [Fact]
    public void Transactions_Single_SenderNull()
    {
        var block = new BlockInfoBuilder().Build();
        var blockSummary = new BlockSummaryBuilder()
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithSender(null)
                .Build())
            .Build();
        _writeRepository.Insert(block, "{}", blockSummary);
        
        var result = _target.Transactions;
        var single = Assert.Single(result);
        Assert.NotNull(single);
        Assert.Null(single.SenderAccountAddress);
    }
}