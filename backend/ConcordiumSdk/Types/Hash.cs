namespace ConcordiumSdk.Types;

public abstract class Hash
{
    private readonly string _formatted;
    private readonly byte[] _value;

    protected Hash(byte[] value)
    {
        if (value.Length != 32) throw new ArgumentException("value must be 32 bytes");
        _value = value;
        _formatted = Convert.ToHexString(value).ToLowerInvariant();
    }

    protected Hash(string value)
    {
        if (value.Length != 64) throw new ArgumentException("string must be 64 char hex string.");
        _value = Convert.FromHexString(value);
        _formatted = value.ToLowerInvariant();
    }

    public string AsString => _formatted;
    public byte[] AsBytes => _value;
    
    public override string ToString()
    {
        return _formatted;
    }

    public override bool Equals(object? obj)
    {
        return obj is Hash other
               && GetType() == obj.GetType()
               && _formatted == other._formatted;
    }

    public override int GetHashCode()
    {
        return _formatted.GetHashCode();
    }

    public static bool operator ==(Hash? left, Hash? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(Hash? left, Hash? right)
    {
        return !Equals(left, right);
    }
}