using Application.Api.GraphQL;
using Application.Api.GraphQL.Import;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using ChainParameters = Application.Api.GraphQL.ChainParameters;
using ChainParametersV0Builder = Tests.TestUtilities.Builders.ChainParametersV0Builder;
using ChainParametersV1Builder = Tests.TestUtilities.Builders.ChainParametersV1Builder;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class ChainParametersWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly ChainParametersWriter _target;
    private readonly ChainParametersV0Builder _chainParametersV0Builder;
    private readonly ChainParametersV1Builder _chainParametersV1Builder;

    public ChainParametersWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new ChainParametersWriter(_dbContextFactory, new NullMetrics());

        _chainParametersV0Builder = new ChainParametersV0Builder()
            .WithFoundationAccountIndex(7);
        
        _chainParametersV1Builder = new ChainParametersV1Builder()
            .WithFoundationAccountIndex(7);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_chain_parameters");
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }
   
    [Fact]
    public async Task GetOrCreateChainParameters_DatabaseEmpty()
    {
        await CreateAccount(7, AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));

        var returnedResult = await WriteV0Data();
        
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(1);
        
        var persistedResult = persistedResults.Single();
        persistedResult.Should().BeOfType<ChainParametersV0>();
        
        returnedResult.Should().Be(persistedResult);

        persistedResult.Should().BeOfType<ChainParametersV0>()
            .Which.FoundationAccountAddress.AsString.Should().Be("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P");
    }
    
    [Fact]
    public async Task GetOrCreateChainParameters_PreviousWrittenIsIdentical()
    {
        // arrange
        await CreateAccount(7, AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        await WriteV0Data();
        
        // act
        var returnedResult = await WriteV0Data();

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
        await CreateAccount(7, AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        var previousWritten = await WriteV0Data();
    
        // act
        _chainParametersV0Builder.WithEuroPerEnergy(17, 4);
            
        var returnedResult = await WriteV0Data();
    
        // Assert that a new row has been written ...
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(2);
        previousWritten.Should().Be(persistedResults[0]);
        returnedResult.Should().Be(persistedResults[1]);
    }
    
    [Fact]
    public async Task GetOrCreateChainParameters_PreviousWrittenIsNotIdentical_ChangedBlockSummaryVersion()
    {
        // arrange
        await CreateAccount(7, AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        var previousWritten = await WriteV0Data();
    
        // act
        var returnedResult = await WriteV1Data();
    
        // Assert that a new row has been written ...
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(2);
        previousWritten.Should().Be(persistedResults[0]);
        returnedResult.Should().Be(persistedResults[1]);
    }
    
    [Fact]
    public async Task GetOrCreateChainParameters_PreviousWrittenIsNotIdentical_FoundationAccountChanged()
    {
        // arrange
        await CreateAccount(7, AccountAddress.From("3XSLuJcXg6xEua6iBPnWacc3iWh93yEDMCqX8FbE3RDSbEnT9P"));
        var previousWritten = await WriteV0Data();
    
        // act
        await CreateAccount(24, AccountAddress.From("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy"));
        _chainParametersV0Builder.WithFoundationAccountIndex(24);
            
        var returnedResult = await WriteV0Data();
    
        // Assert that a new row has been written ...
        var dbContext = _dbContextFactory.CreateDbContext();
        var persistedResults = await dbContext.ChainParameters.ToArrayAsync();
        persistedResults.Length.Should().Be(2);

        persistedResults[0].Should().Be(previousWritten);
        persistedResults[1].Should().Be(returnedResult);

        persistedResults[1].Should().BeOfType<ChainParametersV0>()
            .Which.FoundationAccountAddress.AsString.Should().Be("44B3fpw5duunyeH5U7uxE3N7mpjiBsk9ZwkDiVF9bLNegcVRoy");
    }

    private async Task<ChainParameters> WriteV0Data()
    {
        var blockSummary = new BlockSummaryV0Builder()
            .WithChainParameters(_chainParametersV0Builder.Build())
            .Build();

        var result = await _target.GetOrCreateChainParameters(blockSummary, new ImportState());
        return result.Current;
    }
    
    private async Task<ChainParameters> WriteV1Data()
    {
        var blockSummary = new BlockSummaryV1Builder()
            .WithChainParameters(_chainParametersV1Builder.Build())
            .Build();

        var result = await _target.GetOrCreateChainParameters(blockSummary, new ImportState());
        return result.Current;
    }
    
    private async Task CreateAccount(long accountId, AccountAddress canonicalAccountAddress)
    {
        var account = new AccountBuilder()
            .WithId(accountId)
            .WithCanonicalAddress(canonicalAccountAddress.ToString())
            .WithBaseAddress(canonicalAccountAddress.GetBaseAddress().ToString())
            .Build();

        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Accounts.Add(account);
        await dbContext.SaveChangesAsync();
    }
}