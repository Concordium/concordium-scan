using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class CcdAmountTest
{
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1_000_000)]
    [InlineData(42, 42_000_000)]
    public void FromCcd_FromInt32_Representable(int input, ulong expectedMicroCcd)
    {
        var result = CcdAmount.FromCcd(input);
        Assert.Equal(expectedMicroCcd, result.MicroCcdValue);
    }
    
    [Theory]
    [InlineData(-1)]
    [InlineData(-1_000_000)]
    public void FromCcd_FromInt32_NonRepresentable(int input)
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => CcdAmount.FromCcd(input));
    }
    
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(1_000_000, 1_000_000)]
    [InlineData(42_000_000, 42_000_000)]
    public void FromMicroCcd_FromInt32_Representable(int input, ulong expectedMicroCcd)
    {
        var result = CcdAmount.FromMicroCcd(input);
        Assert.Equal(expectedMicroCcd, result.MicroCcdValue);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(-1_000_000)]
    public void FromMicroCcd_FromInt32_NonRepresentable(int input)
    {
        Assert.ThrowsAny<ArgumentOutOfRangeException>(() => CcdAmount.FromMicroCcd(input));
    }
    
    [Theory]
    [InlineData(0, 0)]
    [InlineData(1, 1)]
    [InlineData(1_000_000, 1_000_000)]
    [InlineData(42_000_000, 42_000_000)]
    public void FromMicroCcd_FromUInt64(UInt64 input, ulong expectedMicroCcd)
    {
        var result = CcdAmount.FromMicroCcd(input);
        Assert.Equal(expectedMicroCcd, result.MicroCcdValue);
    }
    
    [Theory]
    [InlineData(10, 10, true)]
    [InlineData(10, 11, false)]
    public void Equality(ulong value1, ulong value2, bool expectedEqual)
    {
        var target1 = CcdAmount.FromMicroCcd(value1);
        var target2 = CcdAmount.FromMicroCcd(value2);
        
        Assert.Equal(expectedEqual, target1.Equals(target2));
        Assert.Equal(expectedEqual, target2.Equals(target1));
        Assert.Equal(expectedEqual, target1 == target2);
        Assert.Equal(expectedEqual, target2 == target1);
        Assert.Equal(!expectedEqual, target1 != target2);
        Assert.Equal(!expectedEqual, target2 != target1);
    }
}