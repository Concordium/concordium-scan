using System.Buffers.Binary;

namespace ConcordiumSdk.Types;

/// <summary>
/// TODO: Equality, ToString, etc...
/// </summary>
public class Amount
{
    private readonly ulong _microCcd;

    private Amount(ulong microCcd)
    {
        _microCcd = microCcd;
    }

    public ulong MicroCcdValue => _microCcd;

    public static Amount FromMicroCcd(ulong microCcd)
    {
        return new Amount(microCcd);
    }

    public static Amount FromMicroCcd(int microCcd)
    {
        return new Amount(Convert.ToUInt64(microCcd));
    }

    public static Amount FromCcd(int ccd)
    {
        return new Amount(Convert.ToUInt64(ccd * 1_000_000));
    }

    public byte[] SerializeToBytes()
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(bytes), _microCcd);
        return bytes;
    }

    public static Amount operator +(Amount a, Amount b)
        => new(a._microCcd + b._microCcd);
}