using System.Linq;

namespace ConcordiumSdk.Types;

public class ContractEvent
{
    private readonly byte[] _bytes;

    public ContractEvent(byte[] bytes)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public static ContractEvent FromHexString(string hexString)
    {
        var bytes = Convert.FromHexString(hexString);
        return new ContractEvent(bytes);
    }
    
    public byte[] AsBytes => _bytes;
    public string AsHex => Convert.ToHexString(_bytes).ToLowerInvariant();

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;
        
        var other = (ContractEvent)obj;
        return _bytes.SequenceEqual(other._bytes);
    }

    public override int GetHashCode()
    {
        return _bytes.GetHashCode();
    }

    public static bool operator ==(ContractEvent? left, ContractEvent? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(ContractEvent? left, ContractEvent? right)
    {
        return !Equals(left, right);
    }    
}