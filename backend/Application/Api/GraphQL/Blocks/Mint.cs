using HotChocolate;

namespace Application.Api.GraphQL.Blocks;

public class Mint
{
    public ulong BakingReward { get; init; }
    public ulong FinalizationReward { get; init; }
    public ulong PlatformDevelopmentCharge { get; init; }
    [GraphQLDeprecated("Use 'foundationAccountAddress' instead.This field will be removed in the near future.")]
    public string FoundationAccount { get; init; }
    public AccountAddress FoundationAccountAddress => new(FoundationAccount);

}