namespace Application.Api.GraphQL;

public class Mint
{
    public ulong BakingReward { get; init; }
    public ulong FinalizationReward { get; init; }
    public ulong PlatformDevelopmentCharge { get; init; }
    public string FoundationAccount { get; init; }

}