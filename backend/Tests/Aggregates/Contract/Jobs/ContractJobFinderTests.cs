using Application.Aggregates.Contract.Jobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Aggregates.Contract.Jobs;

public class ContractJobFinderTests
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
        var contractJobFinder = new ContractJobFinder(provider);

        // Act
        var contractJobs = contractJobFinder.GetJobs();

        // Assert
        contractJobs.Count().Should().Be(3);
    }
}