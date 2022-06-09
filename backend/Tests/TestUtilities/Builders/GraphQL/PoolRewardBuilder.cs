using Application.Api.GraphQL;
using Application.Api.GraphQL.Bakers;

namespace Tests.TestUtilities.Builders.GraphQL;

public class PoolRewardBuilder
{
    private DateTimeOffset _timestamp = new(2020, 11, 7, 17, 13, 0, 331, TimeSpan.Zero);
    private PoolRewardTarget _pool = new PassiveDelegationPoolRewardTarget();
    private ulong _totalAmount = 1000;
    private ulong _bakerAmount = 800;
    private ulong _delegatorsAmount = 200;

    public PoolReward Build()
    {
        return new PoolReward
        {
            Pool = _pool,
            Index = 0,
            Timestamp = _timestamp,
            RewardType = RewardType.BakerReward,
            TotalAmount = _totalAmount,
            BakerAmount = _bakerAmount,
            DelegatorsAmount = _delegatorsAmount,
            BlockId = 42
        };
    }

    public PoolRewardBuilder WithTimestamp(DateTimeOffset value)
    {
        _timestamp = value;
        return this;
    }

    public PoolRewardBuilder WithPool(PoolRewardTarget value)
    {
        _pool = value;
        return this;
    }

    public PoolRewardBuilder WithAmounts(ulong totalAmount, ulong bakerAmount, ulong delegatorAmount)
    {
        _totalAmount = totalAmount;
        _bakerAmount = bakerAmount;
        _delegatorsAmount = delegatorAmount;
        return this;
    }
}