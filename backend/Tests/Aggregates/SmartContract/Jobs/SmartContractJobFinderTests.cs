using Application.Aggregates.SmartContract.Jobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Aggregates.SmartContract.Jobs;

public class SmartContractJobFinderTests
{
    [Fact]
    public void WhenJobsRegistered_ThenReturnAllJobs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ISmartContractJob>(_ => Mock.Of<ISmartContractJob>());
        services.AddTransient<ISmartContractJob>(_ => Mock.Of<ISmartContractJob>());
        services.AddTransient<ISmartContractJob>(_ => Mock.Of<ISmartContractJob>());
        var provider = services.BuildServiceProvider();
        var smartContractJobFinder = new SmartContractJobFinder(provider);

        // Act
        var smartContractJobs = smartContractJobFinder.GetJobs();

        // Assert
        smartContractJobs.Count().Should().Be(3);
    }
}