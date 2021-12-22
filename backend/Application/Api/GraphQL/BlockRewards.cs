namespace Application.Api.GraphQL;

public class BlockRewards
{
    public long TransactionFees { get; init; }
    public long OldGasAccount { get; init; }
    public long NewGasAccount { get; init; }
    public long BakerReward { get; init; }
    public long FoundationCharge { get; init; }
    public string BakerAccountAddress { get; init; }
    public string FoundationAccountAddress { get; init; }
}