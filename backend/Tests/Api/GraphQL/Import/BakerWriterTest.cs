using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Import;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using BakerDelegationTarget = Application.Api.GraphQL.BakerDelegationTarget;
using PassiveDelegationTarget = Application.Api.GraphQL.PassiveDelegationTarget;

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
        connection.Execute("TRUNCATE TABLE graphql_accounts");
    }

    [Fact]
    public async Task AddOrUpdate_DoesNotExist()
    {
        ulong input = 42;

        var insertCount = 0;
        var updateCount = 0;
        await _target.AddOrUpdateBaker(input,
            item => item,
            item =>
            {
                insertCount++;
                return new Baker
                {
                    Id = (long)item,
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

        ulong input = 7;

        var insertCount = 0;
        var updateCount = 0;
        await _target.AddOrUpdateBaker(input,
            item => item,
            item =>
            {
                insertCount++;
                return new Baker
                {
                    Id = (long)item,
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
    public async Task UpdateBakers()
    {
        await AddBakers(
            new BakerBuilder().WithId(1).WithState(new ActiveBakerStateBuilder().WithStakedAmount(1500).Build()).Build(),
            new BakerBuilder().WithId(2).WithState(new ActiveBakerStateBuilder().WithStakedAmount(200).Build()).Build(),
            new BakerBuilder().WithId(3).WithState(new RemovedBakerStateBuilder().Build()).Build(),
            new BakerBuilder().WithId(4).WithState(new ActiveBakerStateBuilder().WithStakedAmount(700).Build()).Build());

        await _target.UpdateBakers(x => x.ActiveState!.StakedAmount += 400, x => x.ActiveState != null && x.ActiveState.StakedAmount < 1000);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var fromDb = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();
        fromDb.Length.Should().Be(4);
        fromDb[0].ActiveState?.StakedAmount.Should().Be(1500);
        fromDb[1].ActiveState?.StakedAmount.Should().Be(600);
        fromDb[2].ActiveState.Should().BeNull();
        fromDb[3].ActiveState?.StakedAmount.Should().Be(1100);
    }
    
    [Fact]
    public async Task UpdateBakersFromAccountBaker()
    {
        await AddBakers(11, 42);

        var input = new[]
        {
            new AccountBaker
            (
                BakerId: new BakerId(new AccountIndex(11)),
                PendingChange: new AccountBakerRemovePending(_anyDateTimeOffset),
                RestakeEarnings: false,
                StakedAmount: CcdAmount.Zero, 
                null
            )
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

    [Fact]
    public async Task UpdateStakeIfBakerActiveRestakingEarnings_BakerDoesNotExist()
    {
        var bakerStakeUpdate = new AccountRewardSummaryBuilder().WithAccountId(42).WithTotalAmount(100).Build();
        await _target.UpdateStakeIfBakerActiveRestakingEarnings(new[] { bakerStakeUpdate });
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Bakers.ToArrayAsync();
        result.Length.Should().Be(0);
    }
    
    [Fact]
    public async Task UpdateStakeIfBakerActiveRestakingEarnings_BakerIsRemoved()
    {
        await AddBakers(new BakerBuilder().WithId(42).WithState(new RemovedBakerStateBuilder().Build()).Build());

        var bakerStakeUpdate = new AccountRewardSummaryBuilder().WithAccountId(42).WithTotalAmount(100).Build();
        await _target.UpdateStakeIfBakerActiveRestakingEarnings(new[] { bakerStakeUpdate });
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Bakers.SingleAsync();
        result.State.Should().BeOfType<RemovedBakerState>();
    }
    
    [Theory]
    [InlineData(false, 1000)]
    [InlineData(true, 1100)]
    public async Task UpdateStakeIfBakerActiveRestakingEarnings_BakerIsActive(bool restakeEarnings, ulong expectedResult)
    {
        await AddBakers(new BakerBuilder().WithId(42).WithState(new ActiveBakerStateBuilder().WithRestakeRewards(restakeEarnings).WithStakedAmount(1000).Build()).Build());

        var bakerStakeUpdate = new AccountRewardSummaryBuilder().WithAccountId(42).WithTotalAmount(100).Build();
        await _target.UpdateStakeIfBakerActiveRestakingEarnings(new[] { bakerStakeUpdate });
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Bakers.SingleAsync();
        result.State.Should().BeOfType<ActiveBakerState>().Which.StakedAmount.Should().Be(expectedResult);
    }

    [Fact]
    public async Task GetTotalAmountStaked_NoBakers()
    {
        var result = await _target.GetTotalAmountStaked();
        result.Should().Be(0);
    }
 
    [Fact]
    public async Task GetTotalAmountStaked_Bakers()
    {
        await AddBakers(
            new BakerBuilder().WithId(1).WithState(new ActiveBakerStateBuilder().WithStakedAmount(1500).Build()).Build(),
            new BakerBuilder().WithId(2).WithState(new ActiveBakerStateBuilder().WithStakedAmount(200).Build()).Build(),
            new BakerBuilder().WithId(3).WithState(new RemovedBakerStateBuilder().Build()).Build(),
            new BakerBuilder().WithId(4).WithState(new ActiveBakerStateBuilder().WithStakedAmount(700).Build()).Build());

        var result = await _target.GetTotalAmountStaked();
        result.Should().Be(2400);
    }

    [Fact]
    public async Task UpdateDelegatedStake()
    {
        await AddBakers(
            new BakerBuilder().WithId(1).WithState(new ActiveBakerStateBuilder().WithStakedAmount(2500).WithPool(new BakerPoolBuilder().Build()).Build()).Build(),
            new BakerBuilder().WithId(2).WithState(new ActiveBakerStateBuilder().WithStakedAmount(1500).WithPool(new BakerPoolBuilder().Build()).Build()).Build(),
            new BakerBuilder().WithId(3).WithState(new ActiveBakerStateBuilder().WithStakedAmount(1300).WithPool(new BakerPoolBuilder().Build()).Build()).Build(),
            new BakerBuilder().WithId(4).WithState(new ActiveBakerStateBuilder().WithStakedAmount(5000).WithPool(null).Build()).Build(),
            new BakerBuilder().WithId(5).WithState(new RemovedBakerStateBuilder().Build()).Build());
        
        await AddAccounts(
            new AccountBuilder().WithId(20).WithDelegation(new DelegationBuilder().WithStakedAmount(1000).WithDelegationTarget(new BakerDelegationTarget(1)).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(21).WithDelegation(new DelegationBuilder().WithStakedAmount(2000).WithDelegationTarget(new BakerDelegationTarget(1)).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(22).WithDelegation(new DelegationBuilder().WithStakedAmount(5000).WithDelegationTarget(new BakerDelegationTarget(2)).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(23).WithDelegation(new DelegationBuilder().WithStakedAmount(700).WithDelegationTarget(new PassiveDelegationTarget()).Build()).WithUniqueAddress().Build(),
            new AccountBuilder().WithId(24).WithDelegation(null).WithUniqueAddress().Build());
        
        await _target.UpdateDelegatedStake();
        
        await using var context = _dbContextFactory.CreateDbContext();
        var results = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();
        (results[0].State as ActiveBakerState)!.Pool!.DelegatedStake.Should().Be(3000);
        (results[0].State as ActiveBakerState)!.Pool!.TotalStake.Should().Be(5500);
        (results[1].State as ActiveBakerState)!.Pool!.DelegatedStake.Should().Be(5000);
        (results[1].State as ActiveBakerState)!.Pool!.TotalStake.Should().Be(6500);
        (results[2].State as ActiveBakerState)!.Pool!.DelegatedStake.Should().Be(0);
        (results[2].State as ActiveBakerState)!.Pool!.TotalStake.Should().Be(1300);
        (results[3].State as ActiveBakerState)!.Pool.Should().BeNull();
        results[4].State.Should().BeOfType<RemovedBakerState>();
    }

    [Fact]
    public async Task UpdateDelegatedStakeCap()
    {
        await AddBakers(
            new BakerBuilder().WithId(1).WithState(new ActiveBakerStateBuilder().WithStakedAmount(5100).WithPool(new BakerPoolBuilder().Build()).Build()).Build(),
            new BakerBuilder().WithId(2).WithState(new ActiveBakerStateBuilder().WithStakedAmount(4900).WithPool(new BakerPoolBuilder().Build()).Build()).Build(),
            new BakerBuilder().WithId(3).WithState(new ActiveBakerStateBuilder().WithStakedAmount(1000).WithPool(new BakerPoolBuilder().Build()).Build()).Build(),
            new BakerBuilder().WithId(4).WithState(new ActiveBakerStateBuilder().WithStakedAmount(4000).WithPool(null).Build()).Build(),
            new BakerBuilder().WithId(5).WithState(new RemovedBakerStateBuilder().Build()).Build());

        await _target.UpdateDelegatedStakeCap(20000, 0.25m, 3m);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var results = await context.Bakers.OrderBy(x => x.Id).ToArrayAsync();

        (results[0].State as ActiveBakerState)!.Pool!.DelegatedStakeCap.Should().Be(0);
        (results[1].State as ActiveBakerState)!.Pool!.DelegatedStakeCap.Should().Be(133);
        (results[2].State as ActiveBakerState)!.Pool!.DelegatedStakeCap.Should().Be(2000);
        (results[3].State as ActiveBakerState)!.Pool.Should().BeNull();
        results[4].State.Should().BeOfType<RemovedBakerState>();
    }
    
    [Fact]
    public async Task UpdateDelegatedStakeCap_Rounding()
    {
        var baker = new BakerBuilder()
            .WithId(1)
            .WithState(new ActiveBakerStateBuilder()
                .WithStakedAmount(3000000000000)
                .WithPool(new BakerPoolBuilder().Build()).Build())
            .Build();
        
        await AddBakers(baker);

        await _target.UpdateDelegatedStakeCap(15000107677733, 0.25m, 3m);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Bakers.SingleAsync();
        (result.State as ActiveBakerState)!.Pool!.DelegatedStakeCap.Should().Be(1000035892577);
    }
        
    [Fact]
    public async Task UpdateDelegatedStakeCap_PoolHasDelegatedStake()
    {
        var baker = new BakerBuilder()
            .WithId(1)
            .WithState(new ActiveBakerStateBuilder()
                .WithStakedAmount(1000000000000000)
                .WithPool(new BakerPoolBuilder()
                    .WithDelegatedStake(205004302694424)
                    .Build()).Build())
            .Build();
        
        await AddBakers(baker);
    
        await _target.UpdateDelegatedStakeCap(5656544697987840, 0.4m, 3m);
        
        await using var context = _dbContextFactory.CreateDbContext();
        var result = await context.Bakers.SingleAsync();
        (result.State as ActiveBakerState)!.Pool!.DelegatedStakeCap.Should().Be(1967693596862277);
    }
    
    [Fact]
    public async Task GetPaydayPoolStakeSnapshot()
    {
        await AddBakers(
            new BakerBuilder().WithId(1).WithState(new ActiveBakerStateBuilder().WithPool(new BakerPoolBuilder().WithPaydayStatus(new CurrentPaydayStatus { BakerStake = 100, DelegatedStake = 50 }).Build()).Build()).Build(),
            new BakerBuilder().WithId(2).WithState(new ActiveBakerStateBuilder().WithPool(new BakerPoolBuilder().WithPaydayStatus(new CurrentPaydayStatus { BakerStake = 300, DelegatedStake = 200 }).Build()).Build()).Build(),
            new BakerBuilder().WithId(3).WithState(new ActiveBakerStateBuilder().WithPool(null).Build()).Build(),
            new BakerBuilder().WithId(4).WithState(new RemovedBakerStateBuilder().Build()).Build());
        
        var result = await _target.GetPaydayPoolStakeSnapshot();
        result.Items.Length.Should().Be(2);
        var items = result.Items.OrderBy(x => x.BakerId).ToArray();
        items[0].BakerId.Should().Be(1);
        items[0].BakerStake.Should().Be(100);
        items[0].DelegatedStake.Should().Be(50);
        items[1].BakerId.Should().Be(2);
        items[1].BakerStake.Should().Be(300);
        items[1].DelegatedStake.Should().Be(200);
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

    private async Task AddAccounts(params Account[] accounts)
    {
        await using var context = _dbContextFactory.CreateDbContext();
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }
}