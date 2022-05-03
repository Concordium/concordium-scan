namespace Application.Api.GraphQL;

public class MintDistributionV0
{
    public decimal MintPerSlot { get; init; }
    public decimal BakingReward { get; init; }
    public decimal FinalizationReward { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (MintDistributionV0)obj;
        return MintPerSlot == other.MintPerSlot &&
               BakingReward == other.BakingReward &&
               FinalizationReward == other.FinalizationReward;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MintPerSlot, BakingReward, FinalizationReward);
    }

    public static bool operator ==(MintDistributionV0? left, MintDistributionV0? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MintDistributionV0? left, MintDistributionV0? right)
    {
        return !Equals(left, right);
    }
}