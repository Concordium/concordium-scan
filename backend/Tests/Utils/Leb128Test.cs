using System.Collections.Generic;
using Application.Utils;
using FluentAssertions;

namespace Tests.Utils;

public sealed class Leb128Test
{
    [Fact]
    public void WhenLib128Encode_ThenCorrect()
    {
        var tests = new List<(ulong Unsigned, byte[] Bytes)>
        {
            (128, new byte[]{0x80, 0x01}),
            (255UL, new byte[]{0xff, 0x01}),
            (384UL, new byte[]{0x80, 0x03}),
            (511UL, new byte[]{0xff, 0x03}),
            (0UL, new byte[]{0x00}),
        };
        foreach (var (unsigned, bytes) in tests)
        {
            var encodeUnsignedLeb128 = Leb128.EncodeUnsignedLeb128(unsigned);
        
            encodeUnsignedLeb128.ToArray()
                .Should()
                .Equal(bytes);
        }
    }
}
