namespace Application.Api.GraphQL;

public class ContractAddress : Address
{
    public ContractAddress(ulong index, ulong subIndex)
    {
        Index = index;
        SubIndex = subIndex;
    }

    public ulong Index { get; }

    public ulong SubIndex { get; }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        var other = (ContractAddress)obj;
        return Index == other.Index && SubIndex == other.SubIndex;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Index, SubIndex);
    }

    public static bool operator ==(ContractAddress? left, ContractAddress? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ContractAddress? left, ContractAddress? right)
    {
        return !Equals(left, right);
    }
}