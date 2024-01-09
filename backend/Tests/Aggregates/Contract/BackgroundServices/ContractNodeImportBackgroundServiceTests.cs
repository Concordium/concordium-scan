using System.Threading;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.BackgroundServices;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.EventLogs;
using Application.Aggregates.Contract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Configurations;
using Application.Jobs;
using Application.Observability;
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
        const string shouldNotAwait = "should_not_await";
        var services = new ServiceCollection();
        var first = new Mock<IContractJob>();
        first.Setup(j => j.GetUniqueIdentifier())
            .Returns(done);
        first.Setup(j => j.ShouldNodeImportAwait())
            .Returns(true);
        var second = new Mock<IContractJob>();
        second.Setup(j => j.GetUniqueIdentifier())
            .Returns(awaits);
        second.Setup(j => j.ShouldNodeImportAwait())
            .Returns(true);
        var third = new Mock<IContractJob>();
        third.Setup(j => j.GetUniqueIdentifier())
            .Returns(shouldNotAwait);
        third.Setup(j => j.ShouldNodeImportAwait())
            .Returns(false);
        services.AddTransient<IContractJob>(_ => first.Object);
        services.AddTransient<IContractJob>(_ => second.Object);
        services.AddTransient<IContractJob>(_ => third.Object);
        var factory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_fixture.CreateGraphQlDbContext()));
        var provider = services.BuildServiceProvider();
        var contractJobFinder = new JobFinder<IContractJob, ContractJob>(
            provider,
            Options.Create(new GeneralJobOption()),
            factory.Object);
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
            new JobHealthCheck(), 
            Mock.Of<IEventLogHandler>(), 
            Mock.Of<IOptions<FeatureFlagOptions>>());
        
        // Act
        var awaitJobsAsync = await importService.GetJobsToAwait();
        
        // Assert
        awaitJobsAsync.Count.Should().Be(1);
        awaitJobsAsync[0].Should().Be(awaits);
    }
}
