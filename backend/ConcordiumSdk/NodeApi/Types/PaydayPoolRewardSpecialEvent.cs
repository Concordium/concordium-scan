using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class PaydayPoolRewardSpecialEvent : SpecialEvent
{
    /// <summary>
    /// The pool owner (L-Pool when 'None').
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