using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class PaydayPoolRewardBuilder
{
    private DateTimeOffset _timestamp = new(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);
    private PoolRewardTarget _pool = new PassiveDelegationPoolRewardTarget();
    private ulong _sumTotalAmount = 1000;
    private ulong _sumBakerAmount = 800;
    private ulong _sumDelegatorsAmount = 200;

    public PaydayPoolReward Build()
    {
        return new PaydayPoolReward
        {
            Pool = _pool,
            Index = 0,
            Timestamp = _timestamp,
            SumTotalAmount = _sumTotalAmount,
            SumBakerAmount = _sumBakerAmount,
            SumDelegatorsAmount = _sumDelegatorsAmount,
            BlockId = 42
        };
    }

    public PaydayPoolRewardBuilder WithTimestamp(DateTimeOffset value)
    {
        _timestamp = value;
        return this;
    }

    public PaydayPoolRewardBuilder WithPool(PoolRewardTarget value)
    {
        _pool = value;
        return this;
    }

    public PaydayPoolRewardBuilder WithSumAmounts(ulong totalAmount, ulong bakerAmount, ulong delegatorAmount)
    {
        _sumTotalAmount = totalAmount;
        _sumBakerAmount = bakerAmount;
        _sumDelegatorsAmount = delegatorAmount;
        return this;
    }
}