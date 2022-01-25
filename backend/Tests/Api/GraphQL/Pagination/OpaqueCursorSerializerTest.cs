using Application.Api.GraphQL.Pagination;
using FluentAssertions;

namespace Tests.Api.GraphQL.Pagination;

public class OpaqueCursorSerializerTest
{
    [Theory]
    [InlineData(0)]
    [InlineData(12345678)]
    [InlineData(long.MinValue)]
    [InlineData(long.MaxValue)]
    public void RoundTrip(long value)
    {
        var target = new OpaqueCursorSerializer();
        var serialized = target.Serialize(value);
        var deserialized = target.Deserialize(serialized);
        deserialized.Should().Be(value);
    }
}