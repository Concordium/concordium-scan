namespace Application.Api.GraphQL;

public class TransactionFeeDistribution
{
    public decimal Baker { get; init; }
    public decimal GasAccount { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (TransactionFeeDistribution)obj;
        return Baker == other.Baker
               && GasAccount == other.GasAccount;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Baker, GasAccount);
    }

    public static bool operator ==(TransactionFeeDistribution? left, TransactionFeeDistribution? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(TransactionFeeDistribution? left, TransactionFeeDistribution? right)
    {
        return !Equals(left, right);
    }
}