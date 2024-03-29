﻿using Application.Api.GraphQL.Bakers;
using Concordium.Sdk.Types;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders.GraphQL;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;
using BakerPoolOpenStatus = Application.Api.GraphQL.Bakers.BakerPoolOpenStatus;
using CommissionRates = Concordium.Sdk.Types.CommissionRates;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class BakerTest
{
    private readonly ITestOutputHelper _outputHelper;
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyDateTimeOffset = new DateTimeOffset(2010, 10, 1, 12, 23, 34, 124, TimeSpan.Zero);
    private DatabaseFixture _dbFixture;

    public BakerTest(DatabaseFixture dbFixture, ITestOutputHelper outputHelper)
    {
        _dbFixture = dbFixture;
        _outputHelper = outputHelper;
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = DatabaseFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE graphql_bakers");
        
        RefreshBakerStatisticsView();
    }

    private void RefreshBakerStatisticsView()
    {
        using var connection = DatabaseFixture.GetOpenConnection();
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
    public async Task WriteAndReadBaker_ActiveState_PoolNull()
    {
        var entity = new BakerBuilder()
            .WithId(0)
            .WithState(new ActiveBakerStateBuilder().WithPool(null).Build())
            .Build();

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.Single();
        var activeBakerState = result.State.Should().BeOfType<ActiveBakerState>().Subject;
        activeBakerState.Pool.Should().BeNull();
    }
    
    [Fact]
    public async Task WriteAndReadBaker_ActiveState_PoolNotNull()
    {
        var currentPaydayBakerPoolStatus = new CurrentPaydayBakerPoolStatus(
            0,
            false,
            CcdAmount.FromMicroCcd(0),
            CcdAmount.FromMicroCcd(20_000),
            0.13m,
            CcdAmount.FromMicroCcd(21_000),
            CcdAmount.FromMicroCcd(23_000));
        var commissionRates = new CommissionRates(
            AmountFraction.From(0.1m),
            AmountFraction.From(0.2m),
            AmountFraction.From(0.3m)
        );
        var entity = new BakerBuilder()
            .WithId(0)
            .WithState(new ActiveBakerStateBuilder()
                .WithPool(new BakerPoolBuilder()
                    .WithOpenStatus(BakerPoolOpenStatus.ClosedForAll)
                    .WithMetadataUrl("https://example.com/ccd-baker-metadata")
                    .WithCommissionRates(
                        commissionRates.TransactionCommission.AsDecimal(),
                        commissionRates.FinalizationCommission.AsDecimal(),
                        commissionRates.BakingCommission.AsDecimal()
                        )
                    .WithDelegatedStake(1234)
                    .WithDelegatedStakeCap(2233)
                    .WithTotalStake(2000)
                    .WithDelegatorCount(42)
                    .WithPaydayStatus(new CurrentPaydayStatus(
                        currentPaydayBakerPoolStatus,
                        commissionRates
                        ))
                    .Build())
                .Build())
            .Build();

        await AddBaker(entity);
        
        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = dbContext.Bakers.Single();
        var activeBakerState = result.State.Should().BeOfType<ActiveBakerState>().Subject;
        activeBakerState.Pool.Should().NotBeNull();
        activeBakerState.Pool!.OpenStatus.Should().Be(BakerPoolOpenStatus.ClosedForAll);
        activeBakerState.Pool.MetadataUrl.Should().Be("https://example.com/ccd-baker-metadata");
        activeBakerState.Pool.CommissionRates.TransactionCommission.Should().Be(commissionRates.TransactionCommission.AsDecimal());
        activeBakerState.Pool.CommissionRates.FinalizationCommission.Should().Be(commissionRates.FinalizationCommission.AsDecimal());
        activeBakerState.Pool.CommissionRates.BakingCommission.Should().Be(commissionRates.BakingCommission.AsDecimal());
        activeBakerState.Pool.DelegatedStake.Should().Be(1234);
        activeBakerState.Pool.DelegatedStakeCap.Should().Be(2233);
        activeBakerState.Pool.TotalStake.Should().Be(2000);
        activeBakerState.Pool.DelegatorCount.Should().Be(42);
        activeBakerState.Pool.PaydayStatus.Should().NotBeNull();
        activeBakerState.Pool.PaydayStatus!.BakerStake.Should().Be(21000);
        activeBakerState.Pool.PaydayStatus.DelegatedStake.Should().Be(23000);
        activeBakerState.Pool.PaydayStatus.EffectiveStake.Should().Be(20000);
        activeBakerState.Pool.PaydayStatus.LotteryPower.Should().Be(0.13m);
        activeBakerState.Pool.PaydayStatus.CommissionRates.TransactionCommission.Should().Be(commissionRates.TransactionCommission.AsDecimal());
        activeBakerState.Pool.PaydayStatus.CommissionRates.FinalizationCommission.Should().Be(commissionRates.FinalizationCommission.AsDecimal());
        activeBakerState.Pool.PaydayStatus.CommissionRates.BakingCommission.Should().Be(commissionRates.BakingCommission.AsDecimal());          
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
