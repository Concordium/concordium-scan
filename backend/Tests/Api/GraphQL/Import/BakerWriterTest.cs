using Application.Api.GraphQL;
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
    private readonly DateTimeOffset _anyEffectiveTime = new(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public BakerWriterTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture.DatabaseSettings);
        _target = new BakerWriter(_dbContextFactory, new NullMetrics());

        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_bakers");
    }

    [Fact]
    public async Task UpdateBakersFromAccountBaker()
    {
        await AddBakers(11, 42);

        var source = new[]
        {
            new AccountBaker
            {
                BakerId = 11,
                PendingChange = new AccountBakerRemovePending(12)
            }
        };
        
        var returned = await _target.UpdateBakersFromAccountBaker(source, (dst, src) =>
        {
            dst.PendingBakerChange = src.PendingChange switch
            {
                AccountBakerRemovePending => new PendingBakerRemoval(_anyEffectiveTime),
                _ => throw new InvalidOperationException()
            };
        });

        var expectedPendingChange = new PendingBakerRemoval(_anyEffectiveTime);

        returned.Length.Should().Be(1);
        returned[0].PendingBakerChange.Should().Be(expectedPendingChange);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();
        fromDb.Length.Should().Be(2);
        fromDb[0].PendingBakerChange.Should().Be(expectedPendingChange);
        fromDb[1].PendingBakerChange.Should().BeNull();
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
            new BakerBuilder().WithId(7).WithStatus(BakerStatus.Active).WithPendingBakerChange(new PendingBakerRemoval(_anyEffectiveTime.AddMinutes(60))).Build(),
            new BakerBuilder().WithId(13).WithStatus(BakerStatus.Active).WithPendingBakerChange(new PendingBakerRemoval(_anyEffectiveTime.AddMinutes(90))).Build(),
            new BakerBuilder().WithId(21).WithStatus(BakerStatus.Active).WithPendingBakerChange(new PendingBakerRemoval(_anyEffectiveTime.AddMinutes(30))).Build(),
            new BakerBuilder().WithId(54).WithStatus(BakerStatus.Active).WithPendingBakerChange(null).Build());

        await _target.UpdateBakersWithPendingChange(_anyEffectiveTime.AddMinutes(minutesToAdd), baker => baker.Status = BakerStatus.Removed);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();

        fromDb.Where(x => x.Status == BakerStatus.Removed).Select(x => x.Id).Should().Equal(expectedRemovedBakerIds);
        fromDb.Where(x => x.Status == BakerStatus.Active).Select(x => x.Id).Should().Equal(expectedActiveBakerIds);
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
        await AddBakers(
            new BakerBuilder().WithId(7).WithPendingBakerChange(null).Build(),
            new BakerBuilder().WithId(13).WithPendingBakerChange(null).Build(),
            new BakerBuilder().WithId(42).WithPendingBakerChange(null).Build());
        
        var result = await _target.GetMinPendingChangeTime();
        result.Should().BeNull();
    }
    
    [Fact]
    public async Task GetMinPendingChangeTime_PendingChangesExist()
    {
        await AddBakers(
            new BakerBuilder().WithId(7).WithPendingBakerChange(null).Build(),
            new BakerBuilder().WithId(13).WithPendingBakerChange(new PendingBakerRemoval(_anyEffectiveTime.AddMinutes(60))).Build(),
            new BakerBuilder().WithId(42).WithPendingBakerChange(new PendingBakerRemoval(_anyEffectiveTime.AddMinutes(30))).Build());
        
        var result = await _target.GetMinPendingChangeTime();
        result.Should().Be(_anyEffectiveTime.AddMinutes(30));
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