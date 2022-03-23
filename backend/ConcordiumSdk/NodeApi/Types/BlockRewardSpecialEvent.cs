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
    
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        yield return new AccountBalanceUpdate(FoundationAccount, (long)FoundationCharge.MicroCcdValue, BalanceUpdateType.BlockReward);
        yield return new AccountBalanceUpdate(Baker, (long)BakerReward.MicroCcdValue, BalanceUpdateType.BlockReward);
    }
}