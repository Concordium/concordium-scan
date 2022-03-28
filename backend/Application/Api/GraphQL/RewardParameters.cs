namespace Application.Api.GraphQL;

public class RewardParameters
{
    public MintDistribution MintDistribution { get; init; }
    public TransactionFeeDistribution TransactionFeeDistribution { get; init; }
    public GasRewards GasRewards { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (RewardParameters)obj;
        return MintDistribution.Equals(other.MintDistribution) &&
               TransactionFeeDistribution.Equals(other.TransactionFeeDistribution) &&
               GasRewards.Equals(other.GasRewards);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MintDistribution, TransactionFeeDistribution, GasRewards);
    }

    public static bool operator ==(RewardParameters? left, RewardParameters? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RewardParameters? left, RewardParameters? right)
    {
        return !Equals(left, right);
    }
}