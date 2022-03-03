using System.Collections.Generic;
using Application.Common;
using Application.Import.ConcordiumNode;
using FluentAssertions;

namespace Tests.Common;

public class RangeOfLongTest
{
    [Fact]
    public void EnumerableTesting()
    {
        var target = new RangeOfLong(5, 9);
        var result = new List<long>();
        foreach (var item in target)
            result.Add(item);
        result.Should().Equal(5, 6, 7, 8, 9);
    }
}