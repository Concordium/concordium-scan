using System.Threading;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Api.GraphQL.EfCore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using Tests.TestUtilities;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class ContractJobRepositoryTests
{
    private readonly DatabaseFixture _databaseFixture;

    public ContractJobRepositoryTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GivenDoesExistingJobExist_WhenUse_ThenReturnIfInserted(bool insert)
    {
        // Arrange
        const string jobIdentifier = "fooBar";
        await DatabaseFixture.TruncateTables("graphql_contract_jobs");
        if (insert)
        {
            await _databaseFixture.AddAsync(new ContractJob(jobIdentifier));    
        }
        var dbFactory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_databaseFixture.CreateGraphQlDbContext);
        var job = new Mock<IContractJob>();
        job.Setup(j => j.GetUniqueIdentifier())
            .Returns(jobIdentifier);
        var repo = new ContractJobRepository(dbFactory.Object);
        
        // Act
        var doesExistingJobExist = await repo.DoesExistingJobExist(job.Object);
        
        // Arrange
        doesExistingJobExist.Should().Be(insert);
    }

    [Fact]
    public async Task WhenSaveSuccessfullyExecutedJob_ThenStored()
    {
        // Arrange
        const string jobIdentifier = "fooBar";
        await DatabaseFixture.TruncateTables("graphql_contract_jobs");
        var dbFactory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_databaseFixture.CreateGraphQlDbContext);
        var job = new Mock<IContractJob>();
        job.Setup(j => j.GetUniqueIdentifier())
            .Returns(jobIdentifier);
        var repo = new ContractJobRepository(dbFactory.Object);
        
        // Act
        await repo.SaveSuccessfullyExecutedJob(job.Object);

        // Assert
        await using var context = _databaseFixture.CreateGraphQlDbContext();
        var storedJob = await context.ContractJobs
            .SingleAsync(j => j.Job == jobIdentifier);
        storedJob.Job.Should().Be(jobIdentifier);
    }
    
    [Fact]
    public async Task GivenAlreadySavedJob_WhenSaveSuccessfullyExecutedJob_ThenFail()
    {
        // Arrange
        const string jobIdentifier = "fooBar";
        await DatabaseFixture.TruncateTables("graphql_contract_jobs");
        await _databaseFixture.AddAsync(new ContractJob(jobIdentifier));
        var dbFactory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_databaseFixture.CreateGraphQlDbContext);
        var job = new Mock<IContractJob>();
        job.Setup(j => j.GetUniqueIdentifier())
            .Returns(jobIdentifier);
        var repo = new ContractJobRepository(dbFactory.Object);
        
        // Act
        var action = () => repo.SaveSuccessfullyExecutedJob(job.Object);

        // Assert
        await action.Should().ThrowAsync<DbUpdateException>();
    }
}