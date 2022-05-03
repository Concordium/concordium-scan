namespace Application.Api.GraphQL;

public class ChainParametersV1 : ChainParameters
{
    public ulong PoolOwnerCooldown { get; init; }
    public ulong DelegatorCooldown { get; init; }
    public ulong RewardPeriodLength { get; init; }
    public decimal MintPerPayday { get; init; }
    public RewardParametersV1 RewardParameters { get; init; }
    public decimal FinalizationCommissionLPool { get; init; }
    public decimal BakingCommissionLPool { get; init; }
    public decimal TransactionCommissionLPool { get; init; }
    public CommissionRange FinalizationCommissionRange { get; init; }
    public CommissionRange BakingCommissionRange { get; init; }
    public CommissionRange TransactionCommissionRange { get; init; }
    public ulong MinimumEquityCapital { get; init; }
    public decimal CapitalBound { get; init; }
    public LeverageFactor LeverageBound { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (ChainParametersV1)obj;
        return base.Equals(obj) &&
               PoolOwnerCooldown == other.PoolOwnerCooldown &&
               DelegatorCooldown == other.DelegatorCooldown &&
               RewardPeriodLength == other.RewardPeriodLength &&
               MintPerPayday == other.MintPerPayday &&
               RewardParameters.Equals(other.RewardParameters) &&
               FinalizationCommissionLPool == other.FinalizationCommissionLPool &&
               BakingCommissionLPool == other.BakingCommissionLPool &&
               TransactionCommissionLPool == other.TransactionCommissionLPool &&
               FinalizationCommissionRange == other.FinalizationCommissionRange &&
               BakingCommissionRange == other.BakingCommissionRange &&
               TransactionCommissionRange == other.TransactionCommissionRange &&
               MinimumEquityCapital == other.MinimumEquityCapital &&
               CapitalBound == other.CapitalBound &&
               LeverageBound == other.LeverageBound;
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ChainParametersV1? left, ChainParametersV1? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParametersV1? left, ChainParametersV1? right)
    {
        return !Equals(left, right);
    }
}