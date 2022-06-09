using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class PoolRewardTargetToLongConverter : ValueConverter<PoolRewardTarget, long>
{
    public PoolRewardTargetToLongConverter() : base(
        v => ConvertToLong(v),
        v => ConvertToPoolRewardTarget(v))
    {
    }

    public static PoolRewardTarget ConvertToPoolRewardTarget(long value)
    {
        if (value == -1) return new PassiveDelegationPoolRewardTarget();
        return new BakerPoolRewardTarget(value);
    }

    public static long ConvertToLong(PoolRewardTarget value)
    {
        return value switch
        {
            PassiveDelegationPoolRewardTarget => -1,
            BakerPoolRewardTarget x => x.BakerId,
            _ => throw new NotImplementedException()
        };
    }
}