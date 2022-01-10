using System.Linq;

namespace ConcordiumSdk.Types;

public class ContractParameter
{
    private readonly byte[] _bytes;

    public ContractParameter(byte[] bytes)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public static ContractParameter FromHexString(string hexString)
    {
        var bytes = Convert.FromHexString(hexString);
        return new ContractParameter(bytes);
    }
    
    public byte[] AsBytes => _bytes;
    public string AsHex => Convert.ToHexString(_bytes).ToLowerInvariant();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        
        var other = (ContractParameter)obj;
        return _bytes.SequenceEqual(other._bytes);
    }

    public override int GetHashCode()
    {
        return _bytes.GetHashCode();
    }

    public static bool operator ==(ContractParameter? left, ContractParameter? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ContractParameter? left, ContractParameter? right)
    {
        return !Equals(left, right);
    }
}