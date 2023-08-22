using System.Threading;
using Application.Aggregates.SmartContract.Jobs;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Aggregates.SmartContract.Jobs;

public class SmartContractJobFinderTests
{
    [Fact]
    public void WhenJobsRegistered_ThenReturnAllJobs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<ISmartContractJob, First>();
        services.AddTransient<ISmartContractJob, Second>();
        services.AddTransient<ISmartContractJob, Third>();
        var provider = services.BuildServiceProvider();
        var smartContractJobFinder = new SmartContractJobFinder(provider);

        // Act
        var smartContractJobs = smartContractJobFinder.GetJobs();

        // Assert
        smartContractJobs.Count().Should().Be(3);
    }
    
    internal class First : ISmartContractJob
    {
        public Task StartImport(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public string GetUniqueIdentifier()
        {
            throw new NotImplementedException();
        }
    }
    
    internal class Second : ISmartContractJob
    {
        public Task StartImport(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public string GetUniqueIdentifier()
        {
            throw new NotImplementedException();
        }
    }
    
    internal class Third : ISmartContractJob
    {
        public Task StartImport(CancellationToken token)
        {
            throw new NotImplementedException();
        }

        public string GetUniqueIdentifier()
        {
            throw new NotImplementedException();
        }
    }
}