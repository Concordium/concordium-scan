using System.Collections.Generic;
using System.Text;
using ConcordiumSdk.Types;
using FluentAssertions;
using PeterO.Cbor;

namespace Tests.ConcordiumSdk.Types;

public class MemoTest
{
    [Fact]
    public void CreateCborEncodedFromText()
    {
        var target = new Memo(CBORObject.FromObject("hello world").EncodeToBytes());
        target.AsHex.Should().Be("6b68656c6c6f20776f726c64");
    }
    
    [Fact]
    public void TryCborDecodeToText_Success()
    {
        var target = new Memo(CBORObject.FromObject("hello world").EncodeToBytes());
        var result = target.TryCborDecodeToText(out var text);
        result.Should().BeTrue();
        text.Should().Be("hello world");
    }
    
    [Theory]
    [MemberData(nameof(FullByteRangeExcept), parameters: new int[] { 107, 75 })] // 107, 75 is a valid header byte that lets the string be successfully decoded.
    public void TryCborDecodeToText_Failure(byte startByte)
    {
        var utf8EncodedBytes = Encoding.UTF8.GetBytes("hello world");
        var bytes = new[] { startByte }.Concat(utf8EncodedBytes).ToArray();
        var target = new Memo(bytes);
        var result = target.TryCborDecodeToText(out var text);
        result.Should().BeFalse();
        text.Should().BeNull();
    }

    private static IEnumerable<object[]> FullByteRangeExcept(int[] except)
    {
        var allByteValues = Enumerable.Range(0, 255)
            .Except(except)
            .Select(Convert.ToByte);
        
        return allByteValues.Select(b => new object[] {b });
    }

    [Theory]
    [InlineData("63466f6f", "63466f6f", true)]
    [InlineData("63466f6f", "63426172", false)]
    public void Equality(string value1, string value2, bool expectedEqual)
    {
        var target2 = Memo.CreateFromHex(value2);
        var target1 = Memo.CreateFromHex(value1);

        Assert.Equal(expectedEqual, target1.Equals(target2));
        Assert.Equal(expectedEqual, target2.Equals(target1));
        Assert.Equal(expectedEqual, target1 == target2);
        Assert.Equal(expectedEqual, target2 == target1);
        Assert.Equal(!expectedEqual, target1 != target2);
        Assert.Equal(!expectedEqual, target2 != target1);
    }
}