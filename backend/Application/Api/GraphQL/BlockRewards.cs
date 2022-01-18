namespace Application.Api.GraphQL;

public class BlockRewards
{
    public ulong TransactionFees { get; init; }
    public ulong OldGasAccount { get; init; }
    public ulong NewGasAccount { get; init; }
    public ulong BakerReward { get; init; }
    public ulong FoundationCharge { get; init; }
    public string BakerAccountAddress { get; init; }
    public string FoundationAccountAddress { get; init; }
}