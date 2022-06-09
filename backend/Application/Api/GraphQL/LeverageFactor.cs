using HotChocolate;

namespace Application.Api.GraphQL;

public class LeverageFactor
{
    public ulong Numerator { get; init; }
    public ulong Denominator { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (LeverageFactor)obj;
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    public static bool operator ==(LeverageFactor? left, LeverageFactor? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(LeverageFactor? left, LeverageFactor? right)
    {
        return !Equals(left, right);
    }

    [GraphQLIgnore]
    public decimal AsDecimal()
    {
        return (decimal)Numerator / Denominator;
    }
}