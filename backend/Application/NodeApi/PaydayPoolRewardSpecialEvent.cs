using Concordium.Sdk.Types;
using Concordium.Sdk.Types.New;

namespace Application.NodeApi;

public class PaydayPoolRewardSpecialEvent : SpecialEvent
{
    /// <summary>
    /// The pool owner (passive delegators when 'None').
    /// </summary>
    public ulong? PoolOwner { get; init; }
    
    /// <summary>
    /// Accrued transaction fees for pool.
    /// </summary>
    public CcdAmount TransactionFees { get; init; }
    
    /// <summary>
    /// Accrued baking rewards for pool.
    /// </summary>
    public CcdAmount BakerReward { get; init; }
    
    /// <summary>
    /// Accrued finalization rewards for pool.
    /// </summary>
    public CcdAmount FinalizationReward { get; init; }

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return Array.Empty<AccountBalanceUpdate>();
    }
}