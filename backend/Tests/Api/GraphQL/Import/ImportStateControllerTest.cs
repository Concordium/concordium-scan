using System.Transactions;
using Application.Api.GraphQL.Import;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Api.GraphQL.Import;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class ImportStateControllerTest
{
    private readonly ImportStateController _target;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public ImportStateControllerTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new ImportStateController(_dbContextFactory, new NullMetrics());
        
        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_import_state");
    }

    [Fact]
    public async Task GetStateIfExists_NoneExists()
    {
        var result = await _target.GetStateIfExists();
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetStateIfExists_OneExists()
    {
        var state = new ImportState()
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9"
        };
        await AddImportStateToDb(state);

        var result = await _target.GetStateIfExists();
        result.Should().NotBeNull();
        result.GenesisBlockHash.Should().Be("12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9");
    }

    [Fact]
    public async Task SaveChanges_NotPreviouslySaved()
    {
        var state = new ImportState
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9"
        };
        await _target.SaveChanges(state);

        var result = await GetSingleStateFromDb();
        result.Id.Should().NotBe(0);
        result.GenesisBlockHash.Should().Be("12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9");
    }

    [Fact]
    public async Task SaveChanges_UpdatedInstance()
    {
        var state = new ImportState
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9"
        };
        await AddImportStateToDb(state);

        var retrieved = await _target.GetState();
        retrieved.GenesisBlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";
        await _target.SaveChanges(retrieved);
        
        var result = await GetSingleStateFromDb();
        result.GenesisBlockHash.Should().Be("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1");
    }

    /// <summary>
    /// Will retrieve correct data in final get state call, but will be from database!
    /// </summary>
    [Fact]
    public async Task GetState_AfterOnlySavingChanges_TxCommitted()
    {
        var state = new ImportState
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9"
        };
        await AddImportStateToDb(state);

        using (var scope = CreateTransactionScope())
        {
            var retrieved = await _target.GetState();
            retrieved.GenesisBlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";
            await _target.SaveChanges(retrieved);
            
            scope.Complete(); 
        }

        var result = await _target.GetState();
        result.GenesisBlockHash.Should().Be("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1");
    }

    /// <summary>
    /// Will retrieve correct data in final get state call, but this time from cached instance!
    /// </summary>
    [Fact]
    public async Task GetState_AfterSavingChangesAndNotifyOfCommit()
    {
        var state = new ImportState
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9"
        };
        await AddImportStateToDb(state);

        using (var scope = CreateTransactionScope())
        {
            var retrieved = await _target.GetState();
            retrieved.GenesisBlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";
            await _target.SaveChanges(retrieved);
            
            scope.Complete(); 
            _target.SavedChangesCommitted();
        }

        var result = await _target.GetState();
        result.GenesisBlockHash.Should().Be("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1");
    }

    [Fact]
    public async Task GetState_AfterOnlySavingChanges_TxRolledBack()
    {
        var state = new ImportState
        {
            GenesisBlockHash = "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9"
        };
        await AddImportStateToDb(state);

        using (var scope = CreateTransactionScope())
        {
            var retrieved = await _target.GetState();
            retrieved.GenesisBlockHash = "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1";
            await _target.SaveChanges(retrieved);
            
            // No "scope.Complete()" == rollback 
        }

        var result = await _target.GetState();
        result.GenesisBlockHash.Should().Be("12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9");
    }

    private async Task<ImportState> GetSingleStateFromDb()
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        
        return await dbContext.ImportState.SingleAsync();
    }

    private async Task AddImportStateToDb(ImportState state)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.ImportState.Add(state);
        await dbContext.SaveChangesAsync();
    }
    
    private static TransactionScope CreateTransactionScope()
    {
        return new TransactionScope(
            TransactionScopeOption.Required,
            new TransactionOptions
            {
                IsolationLevel = IsolationLevel.ReadCommitted
            },
            TransactionScopeAsyncFlowOption.Enabled);
    }
}