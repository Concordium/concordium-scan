namespace Application.Api.GraphQL;

public class ContractAddress : Address
{
    public ContractAddress(ulong index, ulong subIndex)
    {
        Index = index;
        SubIndex = subIndex;
    }

    /// <summary>
    /// Parses the input string Contract Address in the format <Index, Subindex>
    /// </summary>
    /// <param name="contractAddress">Input string serialized contract address in the format <index, subindex></param>
    public ContractAddress(string contractAddress)
    {
        if (string.IsNullOrWhiteSpace(contractAddress))
        {
            throw new ArgumentNullException(nameof(contractAddress));
        }

        var value = contractAddress.Trim('>', '<', ' ').Split(",");
        if (value.Length != 2)
        {
            throw new ArgumentException("string content is not a contract address.", nameof(contractAddress));
        }

        Index = ulong.Parse(value[0]);
        SubIndex = ulong.Parse(value[1]);
    }

    public ulong Index { get; }

    public ulong SubIndex { get; }
    public string AsString => $"<{Index}, {SubIndex}>";

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
