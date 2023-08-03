using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

[UnionType]
public abstract record DelegationTarget
{
    internal static DelegationTarget From(Concordium.Sdk.Types.DelegationTarget target)
    {
        return target switch
        {
            Concordium.Sdk.Types.BakerDelegationTarget bakerDelegationTarget => 
                BakerDelegationTarget.From(bakerDelegationTarget),
            Concordium.Sdk.Types.PassiveDelegationTarget passiveDelegationTarget => 
                PassiveDelegationTarget.From(passiveDelegationTarget),
            _ => throw new ArgumentOutOfRangeException(nameof(target))
        };
    }
}

public record PassiveDelegationTarget : DelegationTarget
{
    [GraphQLDeprecated("Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields)")]
    public bool Get_() // Will translate to a boolean field named _ in the GraphQL schema.
    {
        return false;
    }

    internal static PassiveDelegationTarget From(Concordium.Sdk.Types.PassiveDelegationTarget _)
    {
        return new PassiveDelegationTarget();
    }
}

public record BakerDelegationTarget(
    long BakerId) : DelegationTarget
{
    internal static BakerDelegationTarget From(Concordium.Sdk.Types.BakerDelegationTarget target) => 
        new((long)target.BakerId.Id.Index);
}