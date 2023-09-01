using Application.Aggregates.Contract.Jobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Aggregates.Contract.Jobs;

public class SmartContractJobFinderTests
{
    [Fact]
    public void WhenJobsRegistered_ThenReturnAllJobs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IContractJob>(_ => Mock.Of<IContractJob>());
        services.AddTransient<IContractJob>(_ => Mock.Of<IContractJob>());
        services.AddTransient<IContractJob>(_ => Mock.Of<IContractJob>());
        var provider = services.BuildServiceProvider();
        var smartContractJobFinder = new ContractJobFinder(provider);

        // Act
        var smartContractJobs = smartContractJobFinder.GetJobs();

        // Assert
        smartContractJobs.Count().Should().Be(3);
    }
}