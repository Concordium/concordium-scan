using System.Linq;

namespace ConcordiumSdk.Types;

public class RegisteredData
{
    private readonly byte[] _bytes;

    public RegisteredData(byte[] bytes)
    {
        _bytes = bytes ?? throw new ArgumentNullException(nameof(bytes));
    }

    public static RegisteredData FromHexString(string hexString)
    {
        var bytes = Convert.FromHexString(hexString);
        return new RegisteredData(bytes);
    }
    
    public byte[] AsBytes => _bytes;
    public string AsHex => Convert.ToHexString(_bytes).ToLowerInvariant();

    protected bool Equals(RegisteredData other)
    {
        return _bytes.SequenceEqual(other._bytes);
    }

    public override bool Equals(object? obj)
    {
        if (ReferenceEquals(null, obj)) return false;
        if (ReferenceEquals(this, obj)) return true;
        if (obj.GetType() != this.GetType()) return false;
        return Equals((RegisteredData)obj);
    }

    public override int GetHashCode()
    {
        return _bytes.GetHashCode();
    }

    public static bool operator ==(RegisteredData? left, RegisteredData? right)
    {
        return Equals(left, right);
    }

    public static bool operator !=(RegisteredData? left, RegisteredData? right)
    {
        return !Equals(left, right);
    }
}