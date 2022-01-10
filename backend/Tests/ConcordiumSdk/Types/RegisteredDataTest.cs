using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class RegisteredDataTest
{
    [Theory]
    [InlineData("63466f6f", "63466f6f", true)]
    [InlineData("63466f6f", "63426172", false)]
    public void Equality(string value1, string value2, bool expectedEqual)
    {
        var target2 = RegisteredData.FromHexString(value2);
        var target1 = RegisteredData.FromHexString(value1);

        Assert.Equal(expectedEqual, target1.Equals(target2));
        Assert.Equal(expectedEqual, target2.Equals(target1));
        Assert.Equal(expectedEqual, target1 == target2);
        Assert.Equal(expectedEqual, target2 == target1);
        Assert.Equal(!expectedEqual, target1 != target2);
        Assert.Equal(!expectedEqual, target2 != target1);
    }
}