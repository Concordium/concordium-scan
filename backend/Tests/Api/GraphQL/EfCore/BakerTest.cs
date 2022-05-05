using Application.Api.GraphQL.Bakers;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.Api.GraphQL.EfCore;

[Collection("Postgres Collection")]
public class BakerTest : IClassFixture<DatabaseFixture>
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);
    private DatabaseFixture _dbFixture;

    public BakerTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _dbFixture = dbFixture;
        _outputHelper = outputHelper;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_bakers");
        
        RefreshBakerStatisticsView();
    }

    private void RefreshBakerStatisticsView()
    {
        using var connection = _dbFixture.GetOpenConnection();
        connection.Execute("refresh materialized view graphql_baker_statistics");
    }

    [Theory]
    [InlineData(0, true)]
    [InlineData(42, false)]
    public async Task WriteAndReadBaker_ActiveState_PendingChangeIsPendingRemoval(long id, bool restakeRewards)
    {
        var entity = new BakerBuilder()
            .WithId(id)
            .WithState(new ActiveBakerStateBuilder()
                .WithStakedAmount(3499)
                .WithPendingChange(new PendingBakerRemoval(_anyDateTimeOffset))
                .WithRestakeRewards(restakeRewards)
                .Build())
            .Build();

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.ToArray();
        result.Length.Should().Be(1);
        result[0].Id.Should().Be(id);
        var state = result[0].State.Should().BeOfType<ActiveBakerState>().Subject;
        state.StakedAmount.Should().Be(3499);
        state.PendingChange.Should().BeOfType<PendingBakerRemoval>().Which.EffectiveTime.Should().Be(_anyDateTimeOffset);
        state.RestakeEarnings.Should().Be(restakeRewards);
    }

    [Fact]
    public async Task WriteAndReadBaker_ActiveState_PendingChangeNull()
    {
        var entity = new BakerBuilder()
            .WithId(0)
            .WithState(new ActiveBakerStateBuilder().WithPendingChange(null).Build())
            .Build();

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.ToArray();
        result.Length.Should().Be(1);
        result[0].Id.Should().Be(0);
        var activeBakerState = result[0].State.Should().BeOfType<ActiveBakerState>().Subject;
        activeBakerState.PendingChange.Should().BeNull();
        activeBakerState.Owner.Should().BeSameAs(result[0]);
    }
    
    [Fact]
    public async Task WriteAndReadBaker_StatisticsNullWhenViewNotRefreshedAfterAdd()
    {
        var entity = new BakerBuilder()
            .WithId(0)
            .WithState(new ActiveBakerStateBuilder().WithPendingChange(null).Build())
            .Build();

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContextWithLog(_outputHelper.WriteLine);
        var conn = dbContext.Database.GetDbConnection();
        var result = dbContext.Bakers.Single();
        result.Statistics.Should().BeNull();
    }

    [Fact]
    public async Task WriteAndReadBaker_StatisticsNotNullWhenViewRefreshedAfterAdd()
    {
        var entity = new BakerBuilder()
            .WithId(0)
            .WithState(new ActiveBakerStateBuilder().WithPendingChange(null).Build())
            .Build();

        await AddBaker(entity);

        RefreshBakerStatisticsView();
        
        await using var dbContext = _dbContextFactory.CreateDbContextWithLog(_outputHelper.WriteLine);
        var conn = dbContext.Database.GetDbConnection();
        var result = dbContext.Bakers.Single();
        result.Statistics.Should().NotBeNull();
    }

    [Fact]
    public async Task WriteAndReadBaker_RemovedState()
    {
        var entity = new BakerBuilder()
            .WithId(0)
            .WithState(new RemovedBakerState(_anyDateTimeOffset))
            .Build();

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.ToArray();
        result.Length.Should().Be(1);
        result[0].Id.Should().Be(0);
        result[0].State.Should().BeOfType<RemovedBakerState>()
            .Which.RemovedAt.Should().Be(_anyDateTimeOffset);
    }

    private async Task AddBaker(Baker entity)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.Bakers.Add(entity);
        await dbContext.SaveChangesAsync();
    }
}