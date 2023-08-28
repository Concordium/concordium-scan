using System.Collections.Generic;
using Application.Aggregates.SmartContract.Jobs;
using FluentAssertions;

namespace Tests.Aggregates.SmartContract.Jobs;

public sealed class SmartContractDatabaseImportJobTests
{
    [Fact]
    public void GivenNonContinuousRange_ThenReturnInternals()
    {
        // Arrange
        var read = new List<ulong> { 0, 2, 3, 4, 7, 8, 10, 12, 14, 15, 16, 20, 21, 22 };

        // Act
        var intervals = SmartContractDatabaseImportJob.PrettifyToRanges(read);

        // Assert
        intervals.Should().BeEquivalentTo(new List<(ulong, ulong)> { (0, 0), (2, 4), (7,8), (10,10), (12,12), (14,16), (20,22) });
    }
    
    [Fact]
    public void GivenContinuousRange_ThenReturnOneInterval()
    {
        // Arrange
        var read = new List<ulong> { 2, 3, 4 };

        // Act
        var intervals = SmartContractDatabaseImportJob.PrettifyToRanges(read);

        // Assert
        intervals.Should().BeEquivalentTo(new List<(ulong, ulong)> { (2, 4) });
    }
}