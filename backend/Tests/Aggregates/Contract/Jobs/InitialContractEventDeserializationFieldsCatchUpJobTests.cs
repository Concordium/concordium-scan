using System.Collections.Generic;
using System.IO;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Aggregates.Contract.Observability;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class InitialContractEventDeserializationFieldsCatchUpJobTests
{
    private readonly DatabaseFixture _databaseFixture;

    public InitialContractEventDeserializationFieldsCatchUpJobTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task WhenUpdateEvent_ThenUpdateInDatabase()
    {
        // Arrange
        var dbFactory = new Mock<IDbContextFactory<GraphQlDbContext>>();
        await using (var context = _databaseFixture.CreateGraphQlDbContext())
        {
            await InsertEvents(context);    
        }
        dbFactory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .Returns(() => Task.FromResult(_databaseFixture.CreateGraphQlDbContext()));
        var options = Options.Create(new ContractAggregateOptions
        {
            Jobs = new Dictionary<string, ContractAggregateJobOptions>
            {
                {
                    InitialContractRejectEventDeserializationFieldsCatchUpJob.JobName,
                    new ContractAggregateJobOptions
                    {
                        BatchSize = 3
                    }
                }
            }
        });
        var job = new InitialContractEventDeserializationFieldsCatchUpJob(dbFactory.Object, options);
        var parallelBatchJob = new ParallelBatchJob<InitialContractEventDeserializationFieldsCatchUpJob>(job, options, new ContractHealthCheck());
        
        // Act
        await parallelBatchJob.StartImport(CancellationToken.None);
        
        // Assert
        await ValidateEventsHasBeenUpdated(_databaseFixture.CreateGraphQlDbContext());
    }

    private async Task ValidateEventsHasBeenUpdated(GraphQlDbContext context)
    {
        const string expectedMessage = "{\"data\":\"\",\"to\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]}}";
        const string expectedEvent = "{\"Mint\":{\"amount\":\"1000000\",\"owner\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]},\"token_id\":\"\"}}";

        await foreach (var contextContractRejectEvent in context.ContractEvents)
        {
            var updateEvent = (contextContractRejectEvent.Event as ContractUpdated)!;
            updateEvent.Message.Should().NotBeNull();
            updateEvent.Message.Should().Be(expectedMessage);
            updateEvent.Events![0].Should().Be(expectedEvent);
        }
    }

    private async Task InsertEvents(GraphQlDbContext context)
    {
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        var contractAddress = new ContractAddress(1,0);
        await AddModule(context, contractAddress);
        await AddContractEvents(context, contractAddress);
    }

    private async Task AddModule(
        GraphQlDbContext context,
        ContractAddress contractAddress
        )
    {
        
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        var moduleReferenceContractLinkEvent = new ModuleReferenceContractLinkEvent(
            0,
            "",
            0,
            0,
            "foo",
            contractAddress,
            new AccountAddress(""),
            ImportSource.NodeImport,
            ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added,
            DateTimeOffset.UtcNow);
        var moduleReferenceEvent = new ModuleReferenceEvent(
            0,
            "",
            0,
            0,
            "foo",
            new AccountAddress(""),
            "",
            schema,
            null,
            ImportSource.NodeImport,
            DateTimeOffset.UtcNow
        );
        await context.AddAsync(moduleReferenceContractLinkEvent);
        await context.AddAsync(moduleReferenceEvent);
        await context.SaveChangesAsync();
    }

    private async Task AddContractEvents(
        GraphQlDbContext context,
        ContractAddress contractAddress
        )
    {
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string message = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        const string eventMessage = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        
        for (var i = 1UL; i <= 20; i++)
        {
            var contractRejectEvent = new ContractEvent(
                i,
                "",
                1,
                1,
                contractAddress,
                new AccountAddress(""),
                new ContractUpdated(
                    contractAddress,
                    new AccountAddress(""),
                    42,
                    message,
                    $"{contractName}.{entrypoint}",
                    ContractVersion.V0,
                    new []{eventMessage}),
                ImportSource.NodeImport,
                DateTimeOffset.UtcNow
            );
            await context.AddAsync(contractRejectEvent);
        }

        await context.SaveChangesAsync();
    }
    
}
