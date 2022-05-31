using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class PoolRewardTargetToLong : ValueConverter<PoolRewardTarget, long>
{
    public PoolRewardTargetToLong() : base(
        v => ConvertToLong(v),
        v => ConvertToPoolRewardTarget(v))
    {
    }
    
    private static PoolRewardTarget ConvertToPoolRewardTarget(long value)
    {
        if (value == -1) return new PassiveDelegationPoolRewardTarget();
        return new BakerPoolRewardTarget(value);
    }

    private static long ConvertToLong(PoolRewardTarget value)
    {
        return value switch
        {
            PassiveDelegationPoolRewardTarget => -1,
            BakerPoolRewardTarget x => x.BakerId,
            _ => throw new NotImplementedException()
        };
    }
}