using HotChocolate;
using HotChocolate.Types;

namespace Application.Api.GraphQL;

[UnionType]
public abstract record DelegationTarget;

public record PassiveDelegationTarget : DelegationTarget
{
    [GraphQLDeprecated("Don't use! This field is only in the schema to make this a valid GraphQL type (which does not allow types without any fields)")]
    public bool Get_() // Will translate to a boolean field named _ in the GraphQL schema.
    {
        return false;
    }
}

public record BakerDelegationTarget(
    long BakerId) : DelegationTarget;