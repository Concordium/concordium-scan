namespace Application.Api.GraphQL;

public class AccountAddress : Address
{
    public AccountAddress(string address)
    {
        Address = address ?? throw new ArgumentNullException(nameof(address));
    }

    public string Address { get; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (AccountAddress)obj;
        return Address == other.Address;
    }

    public override int GetHashCode()
    {
        return Address.GetHashCode();
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