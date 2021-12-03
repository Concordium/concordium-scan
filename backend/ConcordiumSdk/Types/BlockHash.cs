namespace ConcordiumSdk.Types;

public readonly struct BlockHash
{
    private readonly string _formatted;
    private readonly byte[] _value;

    public BlockHash(byte[] value)
    {
        if (value.Length != 32) throw new ArgumentException("value must be 32 bytes");
        _value = value;
        _formatted = Convert.ToHexString(value);
    }

    public BlockHash(string value)
    {
        if (value.Length != 64) throw new ArgumentException("string must be 64 char hex string.");
        _value = Convert.FromHexString(value);
        _formatted = value;
    }

    public string AsString => _formatted;
    public byte[] AsBytes => _value;
    
    public override string ToString()
    {
        return _formatted;
    }

    public bool Equals(BlockHash other)
    {
        return _formatted == other._formatted;
    }

    public override bool Equals(object? obj)
    {
        return obj is BlockHash other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _formatted.GetHashCode();
    }

    public static bool operator ==(BlockHash left, BlockHash right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(BlockHash left, BlockHash right)
    {
        return !left.Equals(right);
    }
}