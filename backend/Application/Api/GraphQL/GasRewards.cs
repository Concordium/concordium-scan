namespace Application.Api.GraphQL;

public class GasRewards
{
    public decimal Baker { get; init; }
    public decimal FinalizationProof { get; init; }
    public decimal AccountCreation { get; init; }
    public decimal ChainUpdate { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (GasRewards)obj;
        return Baker == other.Baker &&
               FinalizationProof == other.FinalizationProof &&
               AccountCreation == other.AccountCreation &&
               ChainUpdate == other.ChainUpdate;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Baker, FinalizationProof, AccountCreation, ChainUpdate);
    }

    public static bool operator ==(GasRewards? left, GasRewards? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(GasRewards? left, GasRewards? right)
    {
        return !Equals(left, right);
    }
}