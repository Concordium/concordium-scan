using Application.Api.GraphQL.EfCore;
using Application.Database;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using AccountAddress = ConcordiumSdk.Types.AccountAddress;

namespace Tests;

[Collection("Postgres Collection")]
public class DataUpdateControllerTest : IClassFixture<DatabaseFixture>
{
    private readonly DataUpdateController _target;
    private readonly GraphQlDbContext2FactoryStub _dbContextFactory;
    private readonly BlockInfoBuilder _blockInfoBuilder = new();
    private readonly BlockSummaryBuilder _blockSummaryBuilder = new();

    public DataUpdateControllerTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContext2FactoryStub(dbFixture.DatabaseSettings);
        _target = new DataUpdateController(_dbContextFactory);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
        connection.Execute("TRUNCATE TABLE graphql_finalization_rewards");
        connection.Execute("TRUNCATE TABLE graphql_baking_rewards");
        connection.Execute("TRUNCATE TABLE graphql_finalization_summary_finalizers");
        connection.Execute("TRUNCATE TABLE graphql_transactions");
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
    public async Task Transactions_BasicInformation_AllValuesNonNull()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithIndex(0)
                .WithSender(new("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"))
                .WithTransactionHash(new("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b"))
                .WithCost(CcdAmount.FromMicroCcd(45872))
                .WithEnergyCost(399)
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var block = dbContext.Blocks.Single();
        var transaction = dbContext.Transactions.Single();
        transaction.Id.Should().BeGreaterThan(0);
        transaction.BlockId.Should().Be(block.Id);
        transaction.TransactionIndex.Should().Be(0);
        transaction.TransactionHash.Should().Be("42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b");
        transaction.SenderAccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        transaction.CcdCost.Should().Be(45872);
        transaction.EnergyCost.Should().Be(399);
    }
    
    [Fact]
    public async Task Transactions_BasicInformation_AllNullableValuesNull()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithSender(null)
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();
        transaction.SenderAccountAddress.Should().BeNull();
    }

    [Theory]
    [InlineData(AccountTransactionType.AddBaker)]
    [InlineData(AccountTransactionType.EncryptedTransfer)]
    [InlineData(AccountTransactionType.SimpleTransfer)]
    [InlineData(AccountTransactionType.TransferWithSchedule)]
    [InlineData(AccountTransactionType.InitializeSmartContractInstance)]
    public async Task Transactions_TransactionType_AccountTransactionTypes(AccountTransactionType transactionType)
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithType(TransactionType.Get(transactionType))
                .Build());

        await WriteData();
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();
        transaction.TransactionType.Should().BeOfType<Application.Api.GraphQL.AccountTransaction>()
            .Which.AccountTransactionType.Should().Be(transactionType);
    }
    
    [Theory]
    [InlineData(CredentialDeploymentTransactionType.Initial)]
    [InlineData(CredentialDeploymentTransactionType.Normal)]
    public async Task Transactions_TransactionType_CredentialDeploymentTransactionTypes(CredentialDeploymentTransactionType transactionType)
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithType(TransactionType.Get(transactionType))
                .Build());

        await WriteData();
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();
        transaction.TransactionType.Should().BeOfType<Application.Api.GraphQL.CredentialDeploymentTransaction>()
            .Which.CredentialDeploymentTransactionType.Should().Be(transactionType);
    }
    
    [Theory]
    [InlineData(UpdateTransactionType.UpdateProtocol)]
    [InlineData(UpdateTransactionType.UpdateLevel1Keys)]
    [InlineData(UpdateTransactionType.UpdateAddIdentityProvider)]
    [InlineData(UpdateTransactionType.UpdateMicroGtuPerEuro)]
    public async Task Transactions_TransactionType_UpdateTransactionTypes(UpdateTransactionType transactionType)
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithType(TransactionType.Get(transactionType))
                .Build());

        await WriteData();
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();
        transaction.TransactionType.Should().BeOfType<Application.Api.GraphQL.UpdateTransaction>()
            .Which.UpdateTransactionType.Should().Be(transactionType);
    }
    
    private async Task WriteData()
    {
        var blockInfo = _blockInfoBuilder.Build();
        var blockSummary = _blockSummaryBuilder.Build();
        await _target.BlockDataReceived(blockInfo, blockSummary);
    }
}

public class GraphQlDbContext2FactoryStub : IDbContextFactory<GraphQlDbContext>
{
    private readonly DatabaseSettings _settings;

    public GraphQlDbContext2FactoryStub(DatabaseSettings settings)
    {
        _settings = settings;
    }

    public GraphQlDbContext CreateDbContext()
    {
        var optionsBuilder = new DbContextOptionsBuilder<GraphQlDbContext>()
            .UseNpgsql(_settings.ConnectionString);
        
        return new GraphQlDbContext(optionsBuilder.Options);
    }
}