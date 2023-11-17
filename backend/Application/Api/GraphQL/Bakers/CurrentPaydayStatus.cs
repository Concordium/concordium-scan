using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Bakers;

/// <summary>
/// Holds current payday values for an given baker.
/// </summary>
public sealed record CurrentPaydayStatus
{
    public ulong BakerStake { get; private init; }
    public ulong DelegatedStake { get; private init; }
    public ulong EffectiveStake { get; private init; }
    public decimal LotteryPower { get; private init; }
    /// <summary>
    /// Holds the current payday commission rates.
    /// </summary>
    public CommissionRates CommissionRates { get; private init; }

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
    /// </summary>
    internal CurrentPaydayStatus(CurrentPaydayBakerPoolStatus source, Concordium.Sdk.Types.CommissionRates rates)
    {
        BakerStake = source.BakerEquityCapital.Value;
        DelegatedStake = source.DelegatedCapital.Value;
        EffectiveStake = source.EffectiveStake.Value;
        LotteryPower = source.LotteryPower;
        CommissionRates = CommissionRates.From(rates);
    }
}
