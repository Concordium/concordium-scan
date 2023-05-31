using System.Globalization;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Import;
using Application.NodeApi;
using FluentAssertions;
using Tests.TestUtilities.Stubs;

namespace Tests.Api.GraphQL.Import;

public class BlockChangeCalculatorTest
{
    private readonly BlockChangeCalculator _target;
    private readonly InitialTokenReleaseScheduleRepositoryStub _initialTokenReleaseScheduleRepositoryStub;

    public BlockChangeCalculatorTest()
    {
        _initialTokenReleaseScheduleRepositoryStub = new InitialTokenReleaseScheduleRepositoryStub();
        _target = new BlockChangeCalculator(_initialTokenReleaseScheduleRepositoryStub);
    }

    [Theory]
    [InlineData("2022-06-01 23:59:59", null)]
    [InlineData("2022-06-02 00:00:00", 1000UL)]
    [InlineData("2022-06-02 23:59:59", 1000UL)]
    [InlineData("2022-06-03 00:00:00", 1900UL)]
    [InlineData("2022-06-04 00:00:00", 1990UL)]
    [InlineData("2022-06-05 00:00:00", 2000UL)]
    [InlineData("2022-06-06 00:00:00", 2000UL)]
    public void CalculateTotalAmountReleased(string slotTimeString, ulong? expectedResult)
    {
        _initialTokenReleaseScheduleRepositoryStub.SetMainnetSchedule(
            new TimestampedAmount(new DateTimeOffset(2022, 6, 2, 0, 0, 0, TimeSpan.Zero), 1000),
            new TimestampedAmount(new DateTimeOffset(2022, 6, 3, 0, 0, 0, TimeSpan.Zero), 1900),
            new TimestampedAmount(new DateTimeOffset(2022, 6, 4, 0, 0, 0, TimeSpan.Zero), 1990),
            new TimestampedAmount(new DateTimeOffset(2022, 6, 5, 0, 0, 0, TimeSpan.Zero), 2000));

        var slotTime = DateTimeOffset.ParseExact(slotTimeString, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal);
        var result = _target.CalculateTotalAmountUnlocked(slotTime, ConcordiumNetworkId.Mainnet.GenesisBlockHash.ToString());
        result.Should().Be(expectedResult);
    }
}
