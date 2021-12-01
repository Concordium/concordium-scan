using System.Buffers.Binary;

namespace ConcordiumSdk.Types;

public class CcdAmount
{
    private readonly ulong _microCcd;

    private CcdAmount(ulong microCcd)
    {
        _microCcd = microCcd;
    }

    public ulong MicroCcdValue => _microCcd;

    public static CcdAmount FromMicroCcd(ulong microCcd)
    {
        return new CcdAmount(microCcd);
    }

    public static CcdAmount FromMicroCcd(int microCcd)
    {
        return new CcdAmount(Convert.ToUInt64(microCcd));
    }

    public static CcdAmount FromCcd(int ccd)
    {
        return new CcdAmount(Convert.ToUInt64(ccd * 1_000_000));
    }

    public byte[] SerializeToBytes()
    {
        var bytes = new byte[8];
        BinaryPrimitives.WriteUInt64BigEndian(new Span<byte>(bytes), _microCcd);
        return bytes;
    }

    public static CcdAmount operator +(CcdAmount a, CcdAmount b)
        => new(a._microCcd + b._microCcd);
}