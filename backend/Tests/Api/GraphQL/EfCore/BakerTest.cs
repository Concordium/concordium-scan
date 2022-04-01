using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class BakerTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;

    public BakerTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_bakers");
    }

    [Theory]
    [InlineData(0, BakerStatus.Active)]
    [InlineData(42, BakerStatus.Removed)]
    public async Task WriteAndReadBaker_PendingChangeNull(int id, BakerStatus bakerStatus)
    {
        var entity = new Baker
        {
            Id = id,
            Status = bakerStatus,
            PendingChange = null, 
        };

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.ToArray();
        result.Length.Should().Be(1);
        result[0].Id.Should().Be(id);
        result[0].Status.Should().Be(bakerStatus);
        result[0].PendingChange.Should().BeNull();
    }
    
    [Fact]
    public async Task WriteAndReadBaker_PendingChangePendingRemoval()
    {
        var dateTimeOffset = new DateTimeOffset(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);
        
        var entity = new Baker
        {
            Id = 10,
            Status = BakerStatus.Active,
            PendingChange = new PendingBakerRemoval(dateTimeOffset) 
        };

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.ToArray();
        result.Length.Should().Be(1);
        result[0].PendingChange.Should().BeOfType<PendingBakerRemoval>()
            .Which.EffectiveTime.Should().Be(dateTimeOffset);
    }

    private async Task AddBaker(Baker entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Bakers.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}