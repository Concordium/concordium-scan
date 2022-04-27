using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class PaydayAccountRewardSpecialEvent : SpecialEvent
{
    /// <summary>
    /// The account that got rewarded.
    /// </summary>
    public AccountAddress Account { get; init; }
    
    /// <summary>
    /// The transaction fee reward at payday to the account.
    /// </summary>
    public CcdAmount TransactionFees { get; init; }
    
    /// <summary>
    /// The baking reward at payday to the account.
    /// </summary>
    public CcdAmount BakerReward { get; init; }
    
    /// <summary>
    /// The finalization reward at payday to the account.
    /// </summary>
    public CcdAmount FinalizationReward { get; init; }

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return new AccountBalanceUpdate[]
        {
            new(Account, (long)TransactionFees.MicroCcdValue, BalanceUpdateType.TransactionFeeReward), 
            new(Account, (long)BakerReward.MicroCcdValue, BalanceUpdateType.BakerReward),
            new(Account, (long)FinalizationReward.MicroCcdValue, BalanceUpdateType.FinalizationReward)
        };
    }
}