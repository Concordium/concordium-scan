using System.Threading;
using Application.Aggregates.SmartContract;
using Application.Aggregates.SmartContract.BackgroundServices;
using Application.Aggregates.SmartContract.Configurations;
using Application.Aggregates.SmartContract.Entities;
using Application.Aggregates.SmartContract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;

namespace Tests.Aggregates.SmartContract.BackgroundServices;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class SmartContractNodeImportBackgroundServiceTests
{
    private readonly DatabaseFixture _fixture;

    public SmartContractNodeImportBackgroundServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task WhenGetJobsToAwait_ThenReturnJobsNotFinished()
    {
        // Assert
        await DatabaseFixture.TruncateTables("graphql_smart_contract_jobs");
        const string done = "done";
        const string awaits = "await";
        var services = new ServiceCollection();
        var first = new Mock<ISmartContractJob>();
        first.Setup(j => j.GetUniqueIdentifier())
            .Returns(done);
        var second = new Mock<ISmartContractJob>();
        second.Setup(j => j.GetUniqueIdentifier())
            .Returns(awaits);
        services.AddTransient<ISmartContractJob>(_ => first.Object);
        services.AddTransient<ISmartContractJob>(_ => second.Object);
        var factory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_fixture.CreateGraphQlDbContext()));
        var provider = services.BuildServiceProvider();
        var smartContractJobFinder = new SmartContractJobFinder(provider);
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            await context.AddAsync(new SmartContractJob(done));
            await context.AddAsync(new SmartContractJob("someOther"));
            await context.SaveChangesAsync();  
        };

        var importService = new SmartContractNodeImportBackgroundService(
            smartContractJobFinder,
            factory.Object,
            Mock.Of<ISmartContractRepositoryFactory>(),
            Mock.Of<ISmartContractNodeClient>(),
            Mock.Of<IFeatureFlags>(),
            Options.Create(new SmartContractAggregateOptions())
        );
        
        // Act
        var awaitJobsAsync = await importService.GetJobsToAwait();
        
        // Assert
        awaitJobsAsync.Count.Should().Be(1);
        awaitJobsAsync[0].Should().Be(awaits);
    }
}