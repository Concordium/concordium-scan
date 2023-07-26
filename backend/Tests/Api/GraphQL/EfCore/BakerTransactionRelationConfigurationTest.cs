using Application.Api.GraphQL.Bakers;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class BakerTransactionRelationConfigurationTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public BakerTransactionRelationConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_baker_transactions");
    }

    [Fact]
    public async Task ReadAndWriteEntity()
    {
        var entity = new BakerTransactionRelation
        {
            BakerId = 42,
            TransactionId = 133
        };

        await AddEntity(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.BakerTransactionRelations.Single();
        result.Should().NotBeNull();
        result.BakerId.Should().Be(42);
        result.Index.Should().BeGreaterThan(0);
        result.TransactionId.Should().Be(133);
    }
    
    private async Task AddEntity(BakerTransactionRelation entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.BakerTransactionRelations.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}