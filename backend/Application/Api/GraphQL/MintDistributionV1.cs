namespace Application.Api.GraphQL;

public class MintDistributionV1
{
    public decimal BakingReward { get; init; }
    public decimal FinalizationReward { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (MintDistributionV1)obj;
        return BakingReward == other.BakingReward &&
               FinalizationReward == other.FinalizationReward;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(BakingReward, FinalizationReward);
    }

    public static bool operator ==(MintDistributionV1? left, MintDistributionV1? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MintDistributionV1? left, MintDistributionV1? right)
    {
        return !Equals(left, right);
    }
}