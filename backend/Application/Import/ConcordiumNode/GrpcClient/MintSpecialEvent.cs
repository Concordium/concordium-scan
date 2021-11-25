namespace Application.Import.ConcordiumNode.GrpcClient;

public class MintSpecialEvent : SpecialEvent
{
    public string MintBakingReward { get; init; }
    public string MintFinalizationReward { get; init; }
    public string MintPlatformDevelopmentCharge { get; init; }
    public string FoundationAccount { get; init; }
}