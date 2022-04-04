using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Import;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

[Collection("Postgres Collection")]
public class BakerWriterTest : IClassFixture<DatabaseFixture>
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly BakerWriter _target;
    private readonly DateTimeOffset _anyDateTimeOffset = new(2021, 09, 16, 10, 21, 33, 452, TimeSpan.Zero);

    public BakerWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new BakerWriter(_dbContextFactory, new NullMetrics());

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_bakers");
    }

    [Fact]
    public async Task AddOrUpdate_DoesNotExist()
    {
        var input = new BakerAddOrUpdateData<long>(42, 42);

        var insertCount = 0;
        var updateCount = 0;
        await _target.AddOrUpdateBaker(input,
            item => item.BakerId,
            item =>
            {
                insertCount++;
                return new Baker
                {
                    Id = (long)item.BakerId,
                    State = new ActiveBakerStateBuilder().Build()
                };
            }, 
            (item, existing) => updateCount++);

        insertCount.Should().Be(1);
        updateCount.Should().Be(0);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var inserted = context.Bakers.Single();
        inserted.Id.Should().Be(42);
        inserted.State.Should().BeOfType<ActiveBakerState>();
    }

    [Fact]
    public async Task AddOrUpdate_Exists()
    {
        await AddBakers(
            new BakerBuilder().WithId(7).WithState(new ActiveBakerStateBuilder().Build()).Build());

        var input = new BakerAddOrUpdateData<long>(7, 7);

        var insertCount = 0;
        var updateCount = 0;
        await _target.AddOrUpdateBaker(input,
            item => item.BakerId,
            item =>
            {
                insertCount++;
                return new Baker
                {
                    Id = (long)item.BakerId,
                    State = new ActiveBakerStateBuilder().Build()
                };
            },
            (s, baker) =>
            {
                updateCount++;
                baker.State = new RemovedBakerState(_anyDateTimeOffset);
            });

        insertCount.Should().Be(0);
        updateCount.Should().Be(1);

        await using var context = _dbContextFactory.CreateDbContext();
        var inserted = context.Bakers.Single();
        inserted.Id.Should().Be(7);
        inserted.State.Should().BeOfType<RemovedBakerState>();
    }
    
    [Fact]
    public async Task UpdateBakersFromAccountBaker()
    {
        await AddBakers(11, 42);

        var input = new[]
        {
            new AccountBaker
            {
                BakerId = 11,
                PendingChange = new AccountBakerRemovePending(12)
            }
        };
        
        var returned = await _target.UpdateBakersFromAccountBaker(input, (dst, src) =>
        {
            if (dst.State is ActiveBakerState active)
                active.PendingChange = new PendingBakerRemoval(_anyDateTimeOffset);
            else
                throw new InvalidOperationException();
        });

        var expectedPendingChange = new PendingBakerRemoval(_anyDateTimeOffset);

        returned.Length.Should().Be(1);
        returned[0].State.Should()
            .BeOfType<ActiveBakerState>().Which
            .PendingChange.Should().Be(expectedPendingChange);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();
        fromDb.Length.Should().Be(2);
        fromDb[0].State.Should()
            .BeOfType<ActiveBakerState>().Which
            .PendingChange.Should().Be(expectedPendingChange);
        
        fromDb[1].State.Should()
            .BeOfType<ActiveBakerState>().Which
            .PendingChange.Should().BeNull();
    }

    [Theory]
    [InlineData(10, new long[] {}, new long[] { 7, 13, 21, 54 })]
    [InlineData(59, new long[] {21}, new long[] { 7, 13, 54 })]
    [InlineData(60, new long[] {7, 21}, new long[] { 13, 54 })]
    [InlineData(61, new long[] {7, 21}, new long[] { 13, 54 })]
    [InlineData(90, new long[] {7, 13, 21}, new long[] { 54 })]
    public async Task UpdateBakersWithPendingChange(int minutesToAdd, long[] expectedRemovedBakerIds, long[] expectedActiveBakerIds)
    {
        await AddBakers(
            new BakerBuilder().WithId(7).WithState(new ActiveBakerStateBuilder().WithPendingChange(new PendingBakerRemoval(_anyDateTimeOffset.AddMinutes(60))).Build()).Build(),
            new BakerBuilder().WithId(13).WithState(new ActiveBakerStateBuilder().WithPendingChange(new PendingBakerRemoval(_anyDateTimeOffset.AddMinutes(90))).Build()).Build(),
            new BakerBuilder().WithId(21).WithState(new ActiveBakerStateBuilder().WithPendingChange(new PendingBakerRemoval(_anyDateTimeOffset.AddMinutes(30))).Build()).Build(),
            new BakerBuilder().WithId(54).WithState(new ActiveBakerStateBuilder().WithPendingChange(null).Build()).Build());

        await _target.UpdateBakersWithPendingChange(_anyDateTimeOffset.AddMinutes(minutesToAdd), baker => baker.State = new RemovedBakerState(_anyDateTimeOffset));
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();

        fromDb.Where(x => x.State is RemovedBakerState).Select(x => x.Id).Should().Equal(expectedRemovedBakerIds);
        fromDb.Where(x => x.State is ActiveBakerState).Select(x => x.Id).Should().Equal(expectedActiveBakerIds);
    }

    [Fact]
    public async Task GetMinPendingChangeTime_NoBakers()
    {
        var result = await _target.GetMinPendingChangeTime();
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetMinPendingChangeTime_NoPendingChanges()
    {
        var activeStateBuilder = new ActiveBakerStateBuilder().WithPendingChange(null);
        
        await AddBakers(
            new BakerBuilder().WithId(7).WithState(activeStateBuilder.Build()).Build(),
            new BakerBuilder().WithId(13).WithState(activeStateBuilder.Build()).Build(),
            new BakerBuilder().WithId(42).WithState(activeStateBuilder.Build()).Build());
        
        var result = await _target.GetMinPendingChangeTime();
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetMinPendingChangeTime_PendingChangesExist()
    {
        await AddBakers(
            new BakerBuilder().WithId(7).WithState(new ActiveBakerStateBuilder().WithPendingChange(null).Build()).Build(),
            new BakerBuilder().WithId(13).WithState(new ActiveBakerStateBuilder().WithPendingChange(new PendingBakerRemoval(_anyDateTimeOffset.AddMinutes(60))).Build()).Build(),
            new BakerBuilder().WithId(42).WithState(new ActiveBakerStateBuilder().WithPendingChange(new PendingBakerRemoval(_anyDateTimeOffset.AddMinutes(30))).Build()).Build());
        
        var result = await _target.GetMinPendingChangeTime();
        result.Should().Be(_anyDateTimeOffset.AddMinutes(30));
    }
    
    private async Task AddBakers(params long[] bakerIds)
    {
        var bakers = bakerIds.Select(id => new BakerBuilder().WithId(id).Build()).ToArray();
        await AddBakers(bakers);
    }

    private async Task AddBakers(params Baker[] bakers)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        context.Bakers.AddRange(bakers);
        await context.SaveChangesAsync();
    }
}