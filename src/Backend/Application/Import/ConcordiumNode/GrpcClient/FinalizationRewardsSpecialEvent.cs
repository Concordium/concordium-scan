namespace Application.Import.ConcordiumNode.GrpcClient;

public class FinalizationRewardsSpecialEvent : SpecialEvent
{
    public FinalizationReward[] FinalizationRewards { get; init; }
    public string Remainder { get; init; }
}