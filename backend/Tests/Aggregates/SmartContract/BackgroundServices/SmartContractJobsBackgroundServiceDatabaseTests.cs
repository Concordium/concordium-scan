using System.Threading;
using Application.Aggregates.SmartContract.BackgroundServices;
using Application.Aggregates.SmartContract.Entities;
using Application.Aggregates.SmartContract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.TestUtilities;

namespace Tests.Aggregates.SmartContract.BackgroundServices;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class SmartContractJobsBackgroundServiceDatabaseTests
{
    private readonly DatabaseFixture _databaseFixture;

    public SmartContractJobsBackgroundServiceDatabaseTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task WhenDoesExistingJobExist_ThenReturnTrueIfPresent(bool exists)
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_smart_contract_jobs");
        const string jobName = "foo";
        var job = new Mock<ISmartContractJob>();
        job.Setup(j => j.GetUniqueIdentifier())
            .Returns(jobName);
        var factory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_databaseFixture.CreateGraphQlDbContext()));
        if (exists)
        {
            await using var context = _databaseFixture.CreateGraphQlDbContext();
            await context.AddAsync(new SmartContractJob(jobName));
            await context.SaveChangesAsync();
        }
        var backgroundService = new SmartContractJobsBackgroundService(
            Mock.Of<ISmartContractJobFinder>(),
            Mock.Of<IFeatureFlags>(),
            factory.Object);
        
        // Act
        var actual = await backgroundService.DoesExistingJobExist(job.Object, CancellationToken.None);
        
        // Assert
        actual.Should().Be(exists);
    }

    [Fact]
    public async Task WhenSaveSuccessfullyExecutedJob_ThenJobSaved()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_smart_contract_jobs");
        const string jobName = "foo";
        var job = new Mock<ISmartContractJob>();
        job.Setup(j => j.GetUniqueIdentifier())
            .Returns(jobName);
        var factory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_databaseFixture.CreateGraphQlDbContext()));
        var backgroundService = new SmartContractJobsBackgroundService(
            Mock.Of<ISmartContractJobFinder>(),
            Mock.Of<IFeatureFlags>(),
            factory.Object);
        
        // Act
        await backgroundService.SaveSuccessfullyExecutedJob(job.Object);
        
        // Assert
        await using var context = _databaseFixture.CreateGraphQlDbContext();
        var jobActual = await context.SmartContractJobs.FirstOrDefaultAsync();
        jobActual.Should().NotBeNull();
        jobActual!.Job.Should().Be(jobName);
    }
}