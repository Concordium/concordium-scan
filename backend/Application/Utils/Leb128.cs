namespace Application.Utils;

/// <summary>
/// https://en.wikipedia.org/wiki/LEB128
/// </summary>
internal static class Leb128
{
    internal static Span<byte> EncodeUnsignedLeb128(ulong value)
    {
        value |= 0;
        var result = new List<byte>();
        do
        { 
            var lowerOrderSevenBitsOfValue = (byte)(value & 0x7f);
            value >>= 7;
            if (value != 0)
            {
                lowerOrderSevenBitsOfValue |= 0x80;
            }
            result.Add(lowerOrderSevenBitsOfValue);
        } while (value != 0);

        return result.ToArray().AsSpan();
    }
}
