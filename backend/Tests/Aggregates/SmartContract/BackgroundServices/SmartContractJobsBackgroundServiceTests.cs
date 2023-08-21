using System.Threading;
using Application.Aggregates.SmartContract.BackgroundServices;
using Application.Aggregates.SmartContract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Tests.Aggregates.SmartContract.BackgroundServices;

public class SmartContractJobsBackgroundServiceTests
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
        var backgroundService = new SmartContractJobsBackgroundService(
            provider,
            Mock.Of<IFeatureFlags>(),
            Mock.Of<IDbContextFactory<GraphQlDbContext>>());
        
        // Act
        var smartContractJobs = backgroundService.GetJobs();

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