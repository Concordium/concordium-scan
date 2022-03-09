using HotChocolate;

namespace Application.Api.GraphQL;

public class AccountAddress : Address
{
    public AccountAddress(string address)
    {
        AsString = address ?? throw new ArgumentNullException(nameof(address));
        Address = address;
    }

    [GraphQLDeprecated("Use 'asString' instead. This field will be removed in the near future.")]
    public string Address { get; }
    public string AsString { get; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (AccountAddress)obj;
        return AsString == other.AsString;
    }

    public override int GetHashCode()
    {
        return AsString.GetHashCode();
    }

    public static bool operator ==(AccountAddress? left, AccountAddress? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(AccountAddress? left, AccountAddress? right)
    {
        return !Equals(left, right);
    }
}