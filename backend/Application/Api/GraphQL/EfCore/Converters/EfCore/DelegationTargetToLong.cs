using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace Application.Api.GraphQL.EfCore.Converters.EfCore;

public class DelegationTargetToLong : ValueConverter<DelegationTarget, long>
{
    public DelegationTargetToLong() : base(
        v => ConvertToString(v),
        v => ConvertToAccountAddress(v))
    {
    }
    
    private static DelegationTarget ConvertToAccountAddress(long value)
    {
        if (value == -1) return new PassiveDelegationTarget();
        return new BakerDelegationTarget((ulong)value);
    }

    private static long ConvertToString(DelegationTarget value)
    {
        return value switch
        {
            PassiveDelegationTarget => -1,
            BakerDelegationTarget x => (long)x.BakerId,
            _ => throw new NotImplementedException()
        };
    }
}