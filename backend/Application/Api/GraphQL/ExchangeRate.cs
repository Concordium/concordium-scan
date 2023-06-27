namespace Application.Api.GraphQL;

public class ExchangeRate
{
    public ulong Numerator { get; init; }
    public ulong Denominator { get; init; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (ExchangeRate)obj;
        return Numerator == other.Numerator && Denominator == other.Denominator;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Numerator, Denominator);
    }

    public static bool operator ==(ExchangeRate? left, ExchangeRate? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ExchangeRate? left, ExchangeRate? right)
    {
        return !Equals(left, right);
    }

    internal static ExchangeRate From(Concordium.Sdk.Types.ExchangeRate exchangeRate)
    {
        return new ExchangeRate
        {
            Numerator = exchangeRate.Numerator,
            Denominator = exchangeRate.Denominator,

        };
    }
}