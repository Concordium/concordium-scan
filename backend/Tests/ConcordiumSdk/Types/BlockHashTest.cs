
using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.Types;

public class BlockHashTest
{
    [Theory]
    [InlineData("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1", "5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1", true)]
    [InlineData("5c0a11302f4098572c4741905b071d958066e0550d03c3186c4483fd920155a1", "12ba993f256c03e805e34d1bbe4f12c255ec1cfc507feedd245543ba5df297e9", false)]
    public void Equality(string address1, string address2, bool expectedEqual)
    {
        var target1 = new BlockHash(address1);
        var target2 = new BlockHash(address2);
        
        Assert.Equal(expectedEqual, target1.Equals(target2));
        Assert.Equal(expectedEqual, target2.Equals(target1));
        Assert.Equal(expectedEqual, target1 == target2);
        Assert.Equal(expectedEqual, target2 == target1);
        Assert.Equal(!expectedEqual, target1 != target2);
        Assert.Equal(!expectedEqual, target2 != target1);
    }
}