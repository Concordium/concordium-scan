using Application.Api.GraphQL.Network;
using FluentAssertions;

public class ClientVersionComparerTest
{
    [Theory]
    [InlineData(null, null, 0)]
    [InlineData("1", null, -1)]
    [InlineData(null, "1", 1)]
    [InlineData("1", "1", 0)]
    [InlineData("2", "1", 1)]
    [InlineData("1", "2", -1)]
    [InlineData("a", "b", -1)]
    [InlineData("b", "a", 1)]
    [InlineData("1.0.0", "2.0.0", -1)]
    [InlineData("3.0.0", "2.0.0", 1)]
    [InlineData("2.0.0", "2.0.0", 0)]
    [InlineData("2.0.0", "10.0.0", -1)]
    public async Task OrderTest(string? v1, string? v2, int expectedResult)
    {
        var actualResult = new ClientVersionComparer().Compare(v1, v2);
        expectedResult.Should().Be(actualResult, String.Format("comparing v1:{0} and v2:{1}", v1, v2));
    }
}