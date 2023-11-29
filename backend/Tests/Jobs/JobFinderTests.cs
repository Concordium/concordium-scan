using System.Threading;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Api.GraphQL.EfCore;
using Application.Configurations;
using Application.Database.MigrationJobs;
using Application.Entities;
using Application.Jobs;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;

namespace Tests.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class JobFinderTests
{
    private readonly DatabaseFixture _fixture;
    
    public JobFinderTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }
    
    [Fact]
    public void WhenJobsRegistered_ThenReturnAllJobs()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddTransient<IContractJob>(_ => Mock.Of<IContractJob>());
        services.AddTransient<IContractJob>(_ => Mock.Of<IContractJob>());
        services.AddTransient<IContractJob>(_ => Mock.Of<IContractJob>());
        var provider = services.BuildServiceProvider();
        var contractJobFinder = new JobFinder<IContractJob, ContractJob>(
            provider,
            Options.Create(new GeneralJobOption()),
            Mock.Of<IDbContextFactory<GraphQlDbContext>>());

        // Act
        var contractJobs = contractJobFinder.GetJobs();

        // Assert
        contractJobs.Count().Should().Be(3);
    }
    
        [Fact]
    public async Task WhenGetJobsToAwait_ThenReturnJobsNotFinished()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_main_migration_jobs");
        const string done = "done";
        const string awaits = "await";
        const string shouldNotAwait = "should_not_await";
        var services = new ServiceCollection();
        var first = new Mock<IMainMigrationJob>();
        first.Setup(j => j.GetUniqueIdentifier())
            .Returns(done);
        first.Setup(j => j.ShouldNodeImportAwait())
            .Returns(true);
        var second = new Mock<IMainMigrationJob>();
        second.Setup(j => j.GetUniqueIdentifier())
            .Returns(awaits);
        second.Setup(j => j.ShouldNodeImportAwait())
            .Returns(true);
        var third = new Mock<IMainMigrationJob>();
        third.Setup(j => j.GetUniqueIdentifier())
            .Returns(shouldNotAwait);
        third.Setup(j => j.ShouldNodeImportAwait())
            .Returns(false);
        services.AddTransient<IMainMigrationJob>(_ => first.Object);
        services.AddTransient<IMainMigrationJob>(_ => second.Object);
        services.AddTransient<IMainMigrationJob>(_ => third.Object);
        var factory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(_fixture.CreateGraphQlDbContext()));
        var provider = services.BuildServiceProvider();
        var contractJobFinder = new JobFinder<IMainMigrationJob, MainMigrationJob>(
            provider,
            Options.Create(new GeneralJobOption()),
            factory.Object
            );
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            await context.AddAsync(new MainMigrationJob(done));
            await context.AddAsync(new MainMigrationJob("someOther"));
            await context.SaveChangesAsync();  
        };
        
        // Act
        var awaitJobsAsync = await contractJobFinder.GetJobsToAwait();
        
        // Assert
        awaitJobsAsync.Count.Should().Be(1);
        awaitJobsAsync[0].Should().Be(awaits);
    }
}
