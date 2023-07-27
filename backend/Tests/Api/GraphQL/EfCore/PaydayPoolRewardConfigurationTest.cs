using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;
using Dapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.EfCore;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class PaydayPoolRewardConfigurationTest
{
    private readonly GraphQlDbContextFactoryStub _dbContextFactory;
    private readonly DateTimeOffset _anyTimestamp = new DateTimeOffset(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);

    public PaydayPoolRewardConfigurationTest(DatabaseFixture dbFixture)
    {
        _dbContextFactory = new GraphQlDbContextFactoryStub(dbFixture. DatabaseSettings);
        
        using var connection = dbFixture.GetOpenConnection();
        connection.Execute("TRUNCATE TABLE metrics_payday_pool_rewards");
    }

    [Fact]
    public async Task WriteAndRead_BakerPool()
    {
        var input = new PaydayPoolReward
        {
            Pool = new BakerPoolRewardTarget(123),
            Timestamp = _anyTimestamp,
            TransactionFeesTotalAmount = 1000,
            TransactionFeesBakerAmount = 750,
            TransactionFeesDelegatorsAmount = 250,
            BakerRewardTotalAmount = 1002,
            BakerRewardBakerAmount = 751,
            BakerRewardDelegatorsAmount = 251,
            FinalizationRewardTotalAmount = 1004,
            FinalizationRewardBakerAmount = 752,
            FinalizationRewardDelegatorsAmount = 252,
            SumTotalAmount = 1006,
            SumBakerAmount = 753,
            SumDelegatorsAmount = 253,
            PaydayDurationSeconds = 2000,
            TotalApy = 0.08,
            BakerApy = 0.09,
            DelegatorsApy = 0.07,
            BlockId = 42,
        };

        await AddPaydayPoolReward(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydayPoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.Pool.Should().BeOfType<BakerPoolRewardTarget>().Which.BakerId.Should().Be(123);
        result.Timestamp.Should().Be(_anyTimestamp);
        result.TransactionFeesTotalAmount.Should().Be(1000);
        result.TransactionFeesBakerAmount.Should().Be(750);
        result.TransactionFeesDelegatorsAmount.Should().Be(250);
        result.BakerRewardTotalAmount.Should().Be(1002);
        result.BakerRewardBakerAmount.Should().Be(751);
        result.BakerRewardDelegatorsAmount.Should().Be(251);
        result.FinalizationRewardTotalAmount.Should().Be(1004);
        result.FinalizationRewardBakerAmount.Should().Be(752);
        result.FinalizationRewardDelegatorsAmount.Should().Be(252);
        result.SumTotalAmount.Should().Be(1006);
        result.SumBakerAmount.Should().Be(753);
        result.SumDelegatorsAmount.Should().Be(253);
        result.PaydayDurationSeconds.Should().Be(2000);
        result.TotalApy.Should().Be(0.08);
        result.BakerApy.Should().Be(0.09);
        result.DelegatorsApy.Should().Be(0.07);
        result.BlockId.Should().Be(42);
    }

    [Fact]
    public async Task WriteAndRead_PassiveDelegation()
    {
        var input = new PaydayPoolReward
        {
            Pool = new PassiveDelegationPoolRewardTarget(),
            Timestamp = _anyTimestamp,
            TransactionFeesTotalAmount = 1000,
            TransactionFeesBakerAmount = 750,
            TransactionFeesDelegatorsAmount = 250,
            BakerRewardTotalAmount = 1002,
            BakerRewardBakerAmount = 751,
            BakerRewardDelegatorsAmount = 251,
            FinalizationRewardTotalAmount = 1004,
            FinalizationRewardBakerAmount = 752,
            FinalizationRewardDelegatorsAmount = 252,
            SumTotalAmount = 1006,
            SumBakerAmount = 753,
            SumDelegatorsAmount = 253,
            BlockId = 42,
        };

        await AddPaydayPoolReward(input);

        await using var dbContext = _dbContextFactory.CreateDbContext();
        var result = await dbContext.PaydayPoolRewards.SingleOrDefaultAsync();
        result.Should().NotBeNull();
        result!.Pool.Should().BeOfType<PassiveDelegationPoolRewardTarget>();
    }

    private async Task AddPaydayPoolReward(PaydayPoolReward input)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext();
        dbContext.PaydayPoolRewards.Add(input);
        await dbContext.SaveChangesAsync();
    }
}