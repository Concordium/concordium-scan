namespace Application.Api.GraphQL;

/// <summary>
/// Gas Reward for Chain Parameter version 2.
/// </summary>
public sealed class GasRewardsCpv2 : IEquatable<GasRewardsCpv2>
{
    public decimal Baker { get; init; }
    public decimal AccountCreation { get; init; }
    public decimal ChainUpdate { get; init; }

    public bool Equals(GasRewardsCpv2? other)
    {
        return other != null &&
               Baker == other.Baker &&
               AccountCreation == other.AccountCreation &&
               ChainUpdate == other.ChainUpdate;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        return obj.GetType() == GetType() && Equals(obj as GasRewardsCpv2);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Baker, AccountCreation, ChainUpdate);
    }

    public static bool operator ==(GasRewardsCpv2? left, GasRewardsCpv2? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(GasRewardsCpv2? left, GasRewardsCpv2? right)
    {
        return !Equals(left, right);
    }
}