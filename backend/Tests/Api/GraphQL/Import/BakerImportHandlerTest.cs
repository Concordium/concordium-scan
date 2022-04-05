using Application.Api.GraphQL.Import;
using FluentAssertions;

namespace Tests.Api.GraphQL.Import;

public class BakerImportHandlerTest
{
    [Fact]
    public void CalculateEffectiveTime_ValuesThatCausedIntOverflow()
    {
        var blockSlotTime = new DateTimeOffset(2021, 12, 22, 13, 32, 33, 0, TimeSpan.Zero);

        var result = BakerImportHandler.CalculateEffectiveTime(1851, blockSlotTime, 24242911);
        
        result.Should().Be(new DateTimeOffset(2021, 12, 29, 13, 00, 25, 250, TimeSpan.Zero));
    }
}