using Concordium.Sdk.Types;

namespace Application.NodeApi;

public class BlockAccrueRewardSpecialEvent : SpecialEvent
{
    /// <summary>
    /// The total fees paid for transactions in the block.
    /// </summary>
    public CcdAmount TransactionFees { get; init; }
    
    /// <summary>
    /// The old balance of the GAS account.
    /// </summary>
    public CcdAmount OldGasAccount { get; init; }
    
    /// <summary>
    /// The new balance of the GAS account.
    /// </summary>
    public CcdAmount NewGasAccount { get; init; }
    
    /// <summary>
    /// The amount awarded to the baker.
    /// </summary>
    public CcdAmount BakerReward { get; init; }
    
    /// <summary>
    /// The amount awarded to the passive delegators
    /// </summary>
    public CcdAmount PassiveReward { get; init; }
    
    /// <summary>
    /// The amount awarded to the foundation.
    /// </summary>
    public CcdAmount FoundationCharge { get; init; }
    
    /// <summary>
    /// The baker of the block, who will receive the award.
    /// </summary>
    public ulong BakerId { get; init; }
    
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return Array.Empty<AccountBalanceUpdate>();
    }
}