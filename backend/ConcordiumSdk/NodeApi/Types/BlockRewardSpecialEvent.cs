using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class BlockRewardSpecialEvent : SpecialEvent
{
    public CcdAmount TransactionFees { get; init; }
    public CcdAmount OldGasAccount { get; init; }
    public CcdAmount NewGasAccount { get; init; }
    public CcdAmount BakerReward { get; init; }
    public CcdAmount FoundationCharge { get; init; }
    public AccountAddress Baker { get; init; }
    public AccountAddress FoundationAccount { get; init; }
}