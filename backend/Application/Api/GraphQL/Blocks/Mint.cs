using Application.Api.GraphQL.Accounts;

namespace Application.Api.GraphQL.Blocks;

public class Mint
{
    public ulong BakingReward { get; init; }
    public ulong FinalizationReward { get; init; }
    public ulong PlatformDevelopmentCharge { get; init; }
    public AccountAddress FoundationAccountAddress { get; init; }

}