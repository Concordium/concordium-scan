using Concordium.Sdk.Types;

namespace Application.Api.GraphQL;

public class CommissionRange
{
    public decimal Min { get; init; }
    public decimal Max { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (CommissionRange)obj;
        return Min == other.Min && Max == other.Max;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    public static bool operator ==(CommissionRange? left, CommissionRange? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(CommissionRange? left, CommissionRange? right)
    {
        return !Equals(left, right);
    }

    internal static CommissionRange From(InclusiveRange<AmountFraction> inclusiveRange) =>
        new()
        {
            Min = inclusiveRange.Min.AsDecimal(),
            Max = inclusiveRange.Min.AsDecimal()
        };
}