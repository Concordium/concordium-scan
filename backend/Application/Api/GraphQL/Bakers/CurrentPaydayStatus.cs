using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Bakers;

/// <summary>
/// Holds current payday values for a given baker.
/// </summary>
public sealed class CurrentPaydayStatus
{
    public ulong BakerStake { get; private set; }
    public ulong DelegatedStake { get; private set; }
    public ulong EffectiveStake { get; private set; }
    public decimal LotteryPower { get; private set; }
    /// <summary>
    /// Holds the current payday commission rates.
    /// </summary>
    public CommissionRates CommissionRates { get; }

    /// <summary>
    /// Needed for EF
    /// </summary>
    private CurrentPaydayStatus() {}
    
    /// <summary>
    /// Create an initial payday status from a genesis block.
    /// </summary>
    internal CurrentPaydayStatus(CcdAmount stakedAmount, Concordium.Sdk.Types.CommissionRates commissionRates)
    {
        BakerStake = stakedAmount.Value;
        DelegatedStake = 0;
        EffectiveStake = 0;
        LotteryPower = 0;
        CommissionRates = CommissionRates.From(commissionRates);
    }

    /// <summary>
    /// Creates a <see cref="CurrentPaydayStatus"/> from data fetched by the node.
    ///
    /// This will be invoked on the first block of a payday if a validator changes from inactive to active.
    /// </summary>
    internal CurrentPaydayStatus(CurrentPaydayBakerPoolStatus source, Concordium.Sdk.Types.CommissionRates rates)
    {
        BakerStake = source.BakerEquityCapital.Value;
        DelegatedStake = source.DelegatedCapital.Value;
        EffectiveStake = source.EffectiveStake.Value;
        LotteryPower = source.LotteryPower;
        CommissionRates = CommissionRates.From(rates);
    }
    
    /// <summary>
    /// Updates the current payday instance with data fetched from node.
    ///
    /// This will be updated once each payday on the first block of the payday.
    /// </summary>
    /// <param name="source">Current payday status fetched from node.</param>
    /// <param name="rates">Current payday commissions fetched from node.</param>
    internal void Update(CurrentPaydayBakerPoolStatus source, Concordium.Sdk.Types.CommissionRates rates)
    {
        BakerStake = source.BakerEquityCapital.Value;
        DelegatedStake = source.DelegatedCapital.Value;
        EffectiveStake = source.EffectiveStake.Value;
        LotteryPower = source.LotteryPower;
        CommissionRates.Update(rates);
    }
}
