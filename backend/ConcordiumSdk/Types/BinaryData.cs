using System.Linq;

namespace ConcordiumSdk.Types;

public class BinaryData
{
    private readonly byte[] _value;

    public BinaryData(byte[] value)
    {
        _value = value ?? throw new ArgumentNullException(nameof(value));
    }

    public static BinaryData FromHexString(string hexString)
    {
        return new BinaryData(Convert.FromHexString(hexString));
    }
    
    public string AsHexString => Convert.ToHexString(_value).ToLowerInvariant();
    public byte[] AsBytes => _value;

    public override string ToString()
    {
        return AsHexString;
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != GetType()) return false;

        var other = (BinaryData)obj;

        return _value.SequenceEqual(other._value);
    }

    public override int GetHashCode()
    {
        return 42; // Not the greatest hash code but will do for this type.
    }

    public static bool operator ==(BinaryData? left, BinaryData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(BinaryData? left, BinaryData? right)
    {
        return !Equals(left, right);
    }
}