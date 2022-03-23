using Application.Api.GraphQL.Import;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using ChainParameters = Application.Api.GraphQL.ChainParameters;
using ExchangeRate = Application.Api.GraphQL.ExchangeRate;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class ChainParametersWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly ChainParametersWriter _target;
    private readonly ChainParametersBuilder _chainParametersBuilder;

    public ChainParametersWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new ChainParametersWriter(_dbContextFactory);

        _chainParametersBuilder = new ChainParametersBuilder()
            .WithElectionDifficulty(0.1m)
            .WithEuroPerEnergy(1, 3)
            .WithMicroGtuPerEuro(2, 5)
            .WithBakerCooldownEpochs(4)
            .WithCredentialsPerBlockLimit(6)
            .WithRewardParameters(new RewardParametersBuilder()
                .WithMintDistribution(0.2m, 0.3m, 0.4m)
                .WithTransactionFeeDistribution(0.5m, 0.6m)
                .WithGasRewards(0.7m, 0.8m, 0.9m, 0.95m)
                .Build())
            .WithFoundationAccountIndex(7)
            .WithMinimumThresholdForBaking(CcdAmount.FromMicroCcd(848482929));
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_chain_parameters");
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }
   
    [Fact]
    public async Task GetOrCreateChainParameters_DatabaseEmpty()
    {
        await CreateAccount(7, new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));

        var returnedResult = await WriteData();
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(1);
        
        var persistedResult = persistedResults.Single();
        persistedResult.Id.Should().BeGreaterThan(0);
        persistedResult.ElectionDifficulty.Should().Be(0.1m);
        persistedResult.EuroPerEnergy.Should().Be(new ExchangeRate { Numerator = 1, Denominator = 3 });
        persistedResult.MicroCcdPerEuro.Should().Be(new ExchangeRate { Numerator = 2, Denominator = 5 });
        persistedResult.BakerCooldownEpochs.Should().Be(4);
        persistedResult.CredentialsPerBlockLimit.Should().Be(6);
        persistedResult.RewardParameters.MintDistribution.MintPerSlot.Should().Be(0.2m);
        persistedResult.RewardParameters.MintDistribution.BakingReward.Should().Be(0.3m);
        persistedResult.RewardParameters.MintDistribution.FinalizationReward.Should().Be(0.4m);
        persistedResult.RewardParameters.TransactionFeeDistribution.Baker.Should().Be(0.5m);
        persistedResult.RewardParameters.TransactionFeeDistribution.GasAccount.Should().Be(0.6m);
        persistedResult.RewardParameters.GasRewards.Baker.Should().Be(0.7m);
        persistedResult.RewardParameters.GasRewards.FinalizationProof.Should().Be(0.8m);
        persistedResult.RewardParameters.GasRewards.AccountCreation.Should().Be(0.9m);
        persistedResult.RewardParameters.GasRewards.ChainUpdate.Should().Be(0.95m);
        persistedResult.FoundationAccountId.Should().Be(7);
        persistedResult.FoundationAccountAddress.AsString.Should().Be("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
        persistedResult.MinimumThresholdForBaking.Should().Be(848482929);

        returnedResult.Should().Be(persistedResult);
    }

    /// <summary>
    /// Values that cannot be stored in (signed) bigint are spotted in the wild.
    /// Therefore we use the less effective "numeric" data type to store these! 
    /// </summary>
    [Fact]
    public async Task GetOrCreateChainParameters_DatabaseEmpty_ExchangeRatesMaxUInt64Value()
    {
        await CreateAccount(7, new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));

        _chainParametersBuilder
            .WithEuroPerEnergy(ulong.MaxValue, ulong.MaxValue)
            .WithMicroGtuPerEuro(ulong.MaxValue, ulong.MaxValue);

        await WriteData();

        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResult = await dbContext.ChainParameters.SingleAsync();

        persistedResult.EuroPerEnergy.Numerator.Should().Be(ulong.MaxValue);
        persistedResult.EuroPerEnergy.Denominator.Should().Be(ulong.MaxValue);
        persistedResult.MicroCcdPerEuro.Numerator.Should().Be(ulong.MaxValue);
        persistedResult.MicroCcdPerEuro.Denominator.Should().Be(ulong.MaxValue);
    }
    
    [Fact]
    public async Task GetOrCreateChainParameters_PreviousWrittenIsIdentical()
    {
        // arrange
        await CreateAccount(7, new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        await WriteData();
        
        // act
        var returnedResult = await WriteData();

        // Assert that no new rows have been written ...
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(1);
        returnedResult.Should().Be(persistedResults.Single());
    }
    
    [Fact]
    public async Task GetOrCreateChainParameters_PreviousWrittenIsNotIdentical()
    {
        // arrange
        await CreateAccount(7, new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        var previousWritten = await WriteData();

        // act
        _chainParametersBuilder.WithEuroPerEnergy(17, 4);
            
        var returnedResult = await WriteData();

        // Assert that a new row has been written ...
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(2);

        persistedResults[1].EuroPerEnergy.Should().Be(new ExchangeRate { Numerator = 17, Denominator = 4 });
        previousWritten.Should().Be(persistedResults[0]);
        returnedResult.Should().Be(persistedResults[1]);
    }
    
    [Fact]
    public async Task GetOrCreateChainParameters_PreviousWrittenIsNotIdentical_FoundationAccountChanged()
    {
        // arrange
        await CreateAccount(7, new AccountAddress("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        await WriteData();

        // act
        await CreateAccount(24, new AccountAddress("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"));
        _chainParametersBuilder.WithFoundationAccountIndex(24);
            
        var returnedResult = await WriteData();

        // Assert that a new row has been written ...
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(2);

        persistedResults[1].FoundationAccountId.Should().Be(24);
        persistedResults[1].FoundationAccountAddress.AsString.Should().Be("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
    }

    private async Task<ChainParameters> WriteData()
    {
        var blockSummary = new BlockSummaryBuilder()
            .WithChainParameters(_chainParametersBuilder.Build())
            .Build();

        return await _target.GetOrCreateChainParameters(blockSummary, new ImportState());
    }
    
    private async Task CreateAccount(long accountId, AccountAddress canonicalAccountAddress)
    {
        var account = new AccountBuilder()
            .WithId(accountId)
            .WithCanonicalAddress(canonicalAccountAddress.AsString)
            .WithBaseAddress(canonicalAccountAddress.GetBaseAddress().AsString)
            .Build();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
    }
}