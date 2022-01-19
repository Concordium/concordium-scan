using Application.Api.GraphQL.EfCore;
using ConcordiumSdk.NodeApi.Types;
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
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly BlockInfoBuilder _blockInfoBuilder = new();
    private readonly BlockSummaryBuilder _blockSummaryBuilder = new();

    public DataUpdateControllerTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new DataUpdateController(_dbContextFactory);

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_blocks");
        connection.Execute("TRUNCATE TABLE graphql_finalization_rewards");
        connection.Execute("TRUNCATE TABLE graphql_baking_rewards");
        connection.Execute("TRUNCATE TABLE graphql_finalization_summary_finalizers");
        connection.Execute("TRUNCATE TABLE graphql_transactions");
        connection.Execute("TRUNCATE TABLE graphql_transaction_events");
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

    [Fact]
    public async Task TransactionEvents_TransactionIdAndIndex()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(
                        new CredentialDeployed("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d", new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")),
                        new AccountCreated(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var transaction = dbContext.Transactions.Single();

        var result = dbContext.TransactionResultEvents.ToArray();
        result.Length.Should().Be(2);
        result[0].TransactionId.Should().Be(transaction.Id);
        result[0].Index.Should().Be(0);
        result[0].Entity.Should().BeOfType<Application.Api.GraphQL.CredentialDeployed>();
        result[1].TransactionId.Should().Be(transaction.Id);
        result[1].Index.Should().Be(1);
        result[1].Entity.Should().BeOfType<Application.Api.GraphQL.AccountCreated>();
    }
    
    [Fact]
    public async Task TransactionEvents_Transferred()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Transferred(CcdAmount.FromMicroCcd(458382), new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), new ContractAddress(234, 32)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.Transferred>();
        result.Amount.Should().Be(458382);
        result.To.Should().Be(new Application.Api.GraphQL.AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        result.From.Should().Be(new Application.Api.GraphQL.ContractAddress(234, 32));
    }
    
    [Fact]
    public async Task TransactionEvents_AccountCreated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new AccountCreated(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.AccountCreated>();
        result.Address.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }
    
    [Fact]
    public async Task TransactionEvents_CredentialDeployed()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new CredentialDeployed("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d", new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.CredentialDeployed>();
        result.RegId.Should().Be("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d");
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_BakerAdded()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new BakerAdded(CcdAmount.FromMicroCcd(12551), true, 17, new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c", "dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88", "823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.BakerAdded>();
        result.StakedAmount.Should().Be(12551);
        result.RestakeEarnings.Should().BeTrue();
        result.BakerId.Should().Be(17);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.SignKey.Should().Be("418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c");
        result.ElectionKey.Should().Be("dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88");
        result.AggregationKey.Should().Be("823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1");
    }

    [Fact]
    public async Task TransactionEvents_BakerKeysUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new BakerKeysUpdated(19, new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c", "dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88", "823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.BakerKeysUpdated>();
        result.BakerId.Should().Be(19);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.SignKey.Should().Be("418dd98d0a42b972b974298e357132214b2821796159bfce86ffeacee567195c");
        result.ElectionKey.Should().Be("dd90b72a8044e1f82443d1531c55078516c912bf3e21633ad7a30309d781cf88");
        result.AggregationKey.Should().Be("823050dc33bd7e94ef46221f45909a2811cb99eef3a41fd9a81a622f1abdc4ef60bac6477bab0f37d000cb077b5cc61f0fa7ffc401ed14f90765d2bea15ea9c2a60010eb0aa8e702ac24f8c25dabe97a53d2d506794e552896f12e43496589f1");
    }

    [Fact]
    public async Task TransactionEvents_BakerRemoved()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new BakerRemoved(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 21))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.BakerRemoved>();
        result.BakerId.Should().Be(21);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_BakerSetRestakeEarnings()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new BakerSetRestakeEarnings(23, new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), true))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.BakerSetRestakeEarnings>();
        result.BakerId.Should().Be(23);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.RestakeEarnings.Should().BeTrue();
    }

    [Fact]
    public async Task TransactionEvents_BakerStakeDecreased()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new BakerStakeDecreased(23, new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(34786451)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.BakerStakeDecreased>();
        result.BakerId.Should().Be(23);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewStakedAmount.Should().Be(34786451);
    }

    [Fact]
    public async Task TransactionEvents_BakerStakeIncreased()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new BakerStakeIncreased(23, new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), CcdAmount.FromMicroCcd(34786451)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.BakerStakeIncreased>();
        result.BakerId.Should().Be(23);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewStakedAmount.Should().Be(34786451);
    }

    [Fact]
    public async Task TransactionEvents_AmountAddedByDecryption()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new AmountAddedByDecryption(CcdAmount.FromMicroCcd(2362462), new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.AmountAddedByDecryption>();
        result.Amount.Should().Be(2362462);
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
    }

    [Fact]
    public async Task TransactionEvents_EncryptedAmountsRemoved()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new EncryptedAmountsRemoved(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2", "acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074", 789))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.EncryptedAmountsRemoved>();
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewEncryptedAmount.Should().Be("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2");
        result.InputAmount.Should().Be("acde243d9f17432a12a04bd553846a9464ecd6c59be5bc3fd6b58d608b002c725c7f495f3c9fe80510d52a739bc5b67280b612dec5a2212bdb3257136fbe5703a3c159a3cda1e70aed0ce69245c8dc6f7c3f374bde1f7584dce9c90b288d3eef8b48cd548dfdeac5d58b0c32585d26c181f142f1e47f9c6695a6abe6a008a7bce1bc02f71f880e198acb03550c50de8daf1e25967487a5f1a9d0ee1afdee9f50c4d2a9fc849d5b234dd47a3af95a7a4e2df78923e39e60ac55d60fd90b4e9074");
        result.UpToIndex.Should().Be(789);
    }

    [Fact]
    public async Task TransactionEvents_EncryptedSelfAmountAdded()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new EncryptedSelfAmountAdded(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2", CcdAmount.FromMicroCcd(23446)))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.EncryptedSelfAmountAdded>();
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewEncryptedAmount.Should().Be("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2");
        result.Amount.Should().Be(23446);
    }

    [Fact]
    public async Task TransactionEvents_NewEncryptedAmount()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new NewEncryptedAmount(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 155, "8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.NewEncryptedAmount>();
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewIndex.Should().Be(155);
        result.EncryptedAmount.Should().Be("8127cc7b219f268461b83c2397573b41815a4c4246b03e17184275ea158561d68bb526a2b5f69eb3ef5c5400927a6c528c461717287f5ec5f31bc0469f1f562f08a270f194963adf814e20fa632782de005efb59014490a2d7a726f2b626d12ab4e23198006317c29cbe3882030ba8f561ba52e6684408ea6e4471871f2f4e043cb2e036bc8e1d53b8d784b61c4cba5ca60c4a8172d9c50f5d56c16640f46f08f1f3224d8fbfa56482547af30b60a21cc24392c1e68df8dcba86bda4e3088fd2");
    }

    [Fact]
    public async Task TransactionEvents_CredentialKeysUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new CredentialKeysUpdated("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d"))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.CredentialKeysUpdated>();
        result.CredId.Should().Be("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d");
    }

    [Fact]
    public async Task TransactionEvents_CredentialsUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new CredentialsUpdated(new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), new []{"b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d"}, new string[0], 123))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.CredentialsUpdated>();
        result.AccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.NewCredIds.Should().Equal("b5e170bfd468a55bb2bf593e7d1904936436679f448779a67d3f8632b92b1c7e7e037bf9175c257f6893d7a80f8b317d");
        result.RemovedCredIds.Should().BeEmpty();
        result.NewThreshold.Should().Be(123);
    }

    [Fact]
    public async Task TransactionEvents_ContractInitialized()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new ContractInitialized(new ModuleRef("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb"), new ContractAddress(1423, 1), CcdAmount.FromMicroCcd(5345462), "init_CIS1-singleNFT", new []{ BinaryData.FromHexString("fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4"), BinaryData.FromHexString("fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00")}))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.ContractInitialized>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
        result.Address.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.Amount.Should().Be(5345462);
        result.InitName.Should().Be("init_CIS1-singleNFT");
        result.EventsAsHex.Should().Equal("fe00010000000000000000736e8b0e5f740321883ee1cf6a75e2d9ba31d3c33cfaf265807b352db91a53c4", "fb00160068747470733a2f2f636f6e636f726469756d2e636f6d00");
    }

    [Fact]
    public async Task TransactionEvents_ContractModuleDeployed()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new ModuleDeployed(new ModuleRef("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.ContractModuleDeployed>();
        result.ModuleRef.Should().Be("2ff7af94aa3e338912d398309531578bd8b7dc903c974111c8d63f4b7098cecb");
    }

    [Fact]
    public async Task TransactionEvents_ContractUpdated()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new Updated(
                        new ContractAddress(1423, 1),
                        new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
                        CcdAmount.FromMicroCcd(15674371),
                        BinaryData.FromHexString("080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"), 
                        "inventory.transfer", 
                        new []
                        {
                            BinaryData.FromHexString("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32"),
                            BinaryData.FromHexString("01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32")
                        }))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.ContractUpdated>();
        result.Address.Should().Be(new Application.Api.GraphQL.ContractAddress(1423, 1));
        result.Instigator.Should().Be(new Application.Api.GraphQL.AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"));
        result.Amount.Should().Be(15674371);
        result.MessageAsHex.Should().Be("080000d671a4d50101c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32");
        result.ReceiveName.Should().Be("inventory.transfer");
        result.EventsAsHex.Should().Equal("05080000d671a4d501aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c90309c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32", "01080000d671a4d50101aa3a794db185bb8ac998abe33146301afcb53f78d58266c6417cb9d859c9030901c0196da50d25f71a236ec71cedc9ba2d49c8c6fc9fa98df7475d3bfbc7612c32");
    }

    [Fact]
    public async Task TransactionEvents_TransferredWithSchedule()
    {
        var baseTimestamp = new DateTimeOffset(2010, 10, 01, 12, 0, 0, TimeSpan.Zero);
        
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferredWithSchedule(
                        new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"), 
                        new AccountAddress("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH"), 
                        new []
                        {
                            new TimestampedAmount(baseTimestamp.AddHours(10), CcdAmount.FromMicroCcd(1000)),
                            new TimestampedAmount(baseTimestamp.AddHours(20), CcdAmount.FromMicroCcd(3333)),
                            new TimestampedAmount(baseTimestamp.AddHours(30), CcdAmount.FromMicroCcd(2111)),
                        }))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.TransferredWithSchedule>();
        result.FromAccountAddress.Should().Be("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        result.ToAccountAddress.Should().Be("3rAsvTuH2gQawenRgwJQzrk9t4Kd2Y1uZYinLqJRDAHZKJKEeH");
        result.AmountsSchedule.Should().Equal(
            new Application.Api.GraphQL.TimestampedAmount(baseTimestamp.AddHours(10), 1000),
            new Application.Api.GraphQL.TimestampedAmount(baseTimestamp.AddHours(20), 3333),
            new Application.Api.GraphQL.TimestampedAmount(baseTimestamp.AddHours(30), 2111));
    }
    
    [Fact]
    public async Task TransactionEvents_DataRegistered()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new DataRegistered(RegisteredData.FromHexString("784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.DataRegistered>();
        result.DataAsHex.Should().Be("784747502d3030323a32636565666132633339396239353639343138353532363032623063383965376665313935303465336438623030333035336339616435623361303365353863");
    }

    [Fact]
    public async Task TransactionEvents_TransferMemo()
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new TransferMemo(Memo.CreateFromHex("704164616d2042696c6c696f6e61697265")))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.TransferMemo>();
        result.RawHex.Should().Be("704164616d2042696c6c696f6e61697265");
    }

    [Fact]
    public async Task TransactionEvents_ChainUpdateEnqueued() // TODO: Still need to add payload!
    {
        _blockSummaryBuilder
            .WithTransactionSummaries(new TransactionSummaryBuilder()
                .WithResult(new TransactionSuccessResultBuilder()
                    .WithEvents(new UpdateEnqueued(new UnixTimeSeconds(1624630671), new MicroGtuPerEuroUpdatePayload(new ExchangeRate(1, 2))))
                    .Build())
                .Build());
        
        await WriteData();

        var result = await ReadSingleTransactionEventType<Application.Api.GraphQL.ChainUpdateEnqueued>();
        result.EffectiveTime.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1624630671));
    }

    private async Task<T> ReadSingleTransactionEventType<T>()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.TransactionResultEvents.SingleAsync();
        return result.Entity.Should().BeOfType<T>().Subject;
    }

    private async Task WriteData()
    {
        var blockInfo = _blockInfoBuilder.Build();
        var blockSummary = _blockSummaryBuilder.Build();
        await _target.BlockDataReceived(blockInfo, blockSummary);
    }
}