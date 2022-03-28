namespace Application.Api.GraphQL;

public class MintDistribution
{
    public decimal MintPerSlot { get; init; }
    public decimal BakingReward { get; init; }
    public decimal FinalizationReward { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (MintDistribution)obj;
        return MintPerSlot == other.MintPerSlot &&
               BakingReward == other.BakingReward &&
               FinalizationReward == other.FinalizationReward;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(MintPerSlot, BakingReward, FinalizationReward);
    }

    public static bool operator ==(MintDistribution? left, MintDistribution? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(MintDistribution? left, MintDistribution? right)
    {
        return !Equals(left, right);
    }
}