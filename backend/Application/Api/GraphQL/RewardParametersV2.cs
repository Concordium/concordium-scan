namespace Application.Api.GraphQL;

/// <summary>
/// Reward parameters for Chain Parameters version 2.
/// </summary>
public sealed class RewardParametersV2 : IEquatable<RewardParametersV2>
{
    public MintDistributionV1 MintDistribution { get; init; }
    public TransactionFeeDistribution TransactionFeeDistribution { get; init; }
    public GasRewardsCpv2 GasRewards { get; init; }

    public bool Equals(RewardParametersV2? other)
    {
        return other != null && 
               MintDistribution.Equals(other.MintDistribution) &&
               TransactionFeeDistribution.Equals(other.TransactionFeeDistribution) &&
               GasRewards.Equals(other.GasRewards);
    }
    
    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as RewardParametersV2);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MintDistribution, TransactionFeeDistribution, GasRewards);
    }

    public static bool operator ==(RewardParametersV2? left, RewardParametersV2? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RewardParametersV2? left, RewardParametersV2? right)
    {
        return !Equals(left, right);
    }
}