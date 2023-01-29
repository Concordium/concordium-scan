using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class CcdAmountTest
{
    [Theory]
    [InlineData(0, 0UL)]
    [InlineData(1, 1_000_000UL)]
    [InlineData(42, 42_000_000UL)]
    [InlineData(2_147_483_647, 2_147_483_647_000_000UL)]
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

    [Theory]
    [InlineData(199, 200, true)]
    [InlineData(200, 200, false)]
    [InlineData(201, 200, false)]
    public void Operator_LessThan(int leftValue, int rightValue, bool expectedResult)
    {
        var left = CcdAmount.FromMicroCcd(leftValue);
        var right = CcdAmount.FromMicroCcd(rightValue);

        var result = left < right;
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(199, 200, true)]
    [InlineData(200, 200, true)]
    [InlineData(201, 200, false)]
    public void Operator_LessThanEqual(int leftValue, int rightValue, bool expectedResult)
    {
        var left = CcdAmount.FromMicroCcd(leftValue);
        var right = CcdAmount.FromMicroCcd(rightValue);

        var result = left <= right;
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(199, 200, false)]
    [InlineData(200, 200, false)]
    [InlineData(201, 200, true)]
    public void Operator_GreaterThan(int leftValue, int rightValue, bool expectedResult)
    {
        var left = CcdAmount.FromMicroCcd(leftValue);
        var right = CcdAmount.FromMicroCcd(rightValue);

        var result = left > right;
        Assert.Equal(expectedResult, result);
    }
    
    [Theory]
    [InlineData(199, 200, false)]
    [InlineData(200, 200, true)]
    [InlineData(201, 200, true)]
    public void Operator_GreaterThanEqual(int leftValue, int rightValue, bool expectedResult)
    {
        var left = CcdAmount.FromMicroCcd(leftValue);
        var right = CcdAmount.FromMicroCcd(rightValue);

        var result = left >= right;
        Assert.Equal(expectedResult, result);
    }

    [Theory]
    [InlineData(1, "0.000001")]
    [InlineData(20, "0.00002")]
    [InlineData(23, "0.000023")]
    [InlineData(624, "0.000624")]
    [InlineData(9183, "0.009183")]
    [InlineData(43891, "0.043891")]
    [InlineData(123877, "0.123877")]
    [InlineData(1000000, "1")]
    [InlineData(1000001, "1.000001")]
    [InlineData(3000000, "3")]
    [InlineData(10000000, "10")]
    public void FormattedCcd(ulong microCcd, string expectedResult)
    {
        var result = CcdAmount.FromMicroCcd(microCcd).FormattedCcd;
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public void Operator_Multiply_ByInt()
    {
        var result1 = CcdAmount.FromMicroCcd(500) * 100;
        var result2 = 100 * CcdAmount.FromMicroCcd(500);
        Assert.Equal(CcdAmount.FromMicroCcd(50000), result1);
        Assert.Equal(CcdAmount.FromMicroCcd(50000), result2);
    }
}