using System.Threading;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.BackgroundServices;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Aggregates.Contract.Observability;
using Application.Api.GraphQL.EfCore;
using Application.Configurations;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;

namespace Tests.Aggregates.Contract.BackgroundServices;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class ContractNodeImportBackgroundServiceTests
{
    private readonly DatabaseFixture _fixture;

    public ContractNodeImportBackgroundServiceTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public async Task WhenGetJobsToAwait_ThenReturnJobsNotFinished()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_jobs");
        const string done = "done";
        const string awaits = "await";
        var services = new ServiceCollection();
        var first = new Mock<IContractJob>();
        first.Setup(j => j.GetUniqueIdentifier())
            .Returns(done);
        var second = new Mock<IContractJob>();
        second.Setup(j => j.GetUniqueIdentifier())
            .Returns(awaits);
        services.AddTransient<IContractJob>(_ => first.Object);
        services.AddTransient<IContractJob>(_ => second.Object);
        var factory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_fixture.CreateGraphQlDbContext()));
        var provider = services.BuildServiceProvider();
        var contractJobFinder = new ContractJobFinder(provider);
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            await context.AddAsync(new ContractJob(done));
            await context.AddAsync(new ContractJob("someOther"));
            await context.SaveChangesAsync();  
        };

        var importService = new ContractNodeImportBackgroundService(
            contractJobFinder,
            factory.Object,
            Mock.Of<IContractRepositoryFactory>(),
            Mock.Of<IContractNodeClient>(),
            Options.Create(new ContractAggregateOptions()),
            new ContractHealthCheck(),
            Mock.Of<IOptions<FeatureFlagOptions>>());
        
        // Act
        var awaitJobsAsync = await importService.GetJobsToAwait();
        
        // Assert
        awaitJobsAsync.Count.Should().Be(1);
        awaitJobsAsync[0].Should().Be(awaits);
    }
}
