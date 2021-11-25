namespace Application.Import.ConcordiumNode.GrpcClient;

public class BlockRewardSpecialEvent : SpecialEvent
{
    public string TransactionFees { get; init; }
    public string OldGasAccount { get; init; }
    public string NewGasAccount { get; init; }
    public string BakerReward { get; init; }
    public string FoundationCharge { get; init; }
    public string Baker { get; init; }
    public string FoundationAccount { get; init; }
}