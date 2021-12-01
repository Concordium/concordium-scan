namespace ConcordiumSdk.Types;

public readonly struct Nonce 
{
    private readonly ulong _value;

    public Nonce(ulong value)
    {
        _value = value;
    }

    public ulong AsUInt64 => _value;

    public bool Equals(Nonce other)
    {
        return _value == other._value;
    }

    public override bool Equals(object? obj)
    {
        return obj is Nonce other && Equals(other);
    }

    public override int GetHashCode()
    {
        return _value.GetHashCode();
    }

    public static bool operator ==(Nonce left, Nonce right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(Nonce left, Nonce right)
    {
        return !left.Equals(right);
    }

    public override string ToString()
    {
        return _value.ToString();
    }
}