namespace Application.Api.GraphQL;

public class ChainParametersV0 : ChainParameters
{
    public ulong BakerCooldownEpochs { get; init; }
    public RewardParametersV0 RewardParameters { get; init; }
    public ulong MinimumThresholdForBaking { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (ChainParametersV0)obj;
        return base.Equals(obj) &&
               BakerCooldownEpochs == other.BakerCooldownEpochs &&
               RewardParameters.Equals(other.RewardParameters) &&
               MinimumThresholdForBaking == other.MinimumThresholdForBaking;
    }

    public override int GetHashCode()
    {
        return Id;
    }

    public static bool operator ==(ChainParametersV0? left, ChainParametersV0? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ChainParametersV0? left, ChainParametersV0? right)
    {
        return !Equals(left, right);
    }
}