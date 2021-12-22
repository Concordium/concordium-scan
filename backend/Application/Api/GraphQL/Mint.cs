namespace Application.Api.GraphQL;

public class Mint
{
    public long BakingReward { get; init; }
    public long FinalizationReward { get; init; }
    public long PlatformDevelopmentCharge { get; init; }
    public string FoundationAccount { get; init; }

}