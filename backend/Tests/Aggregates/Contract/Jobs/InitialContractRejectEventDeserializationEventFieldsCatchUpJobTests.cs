using System.Collections.Generic;
using System.IO;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Extensions;
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
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class InitialContractRejectEventDeserializationEventFieldsCatchUpJobTests
{
    private readonly DatabaseFixture _databaseFixture;

    public InitialContractRejectEventDeserializationEventFieldsCatchUpJobTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    [Fact]
    public async Task WhenUpdateEvent_ThenUpdateInDatabase()
    {
        // Arrange
        ContractExtensions.AddDapperTypeHandlers();
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
                    InitialContractRejectEventDeserializationEventFieldsCatchUpJob.JobName,
                    new ContractAggregateJobOptions
                    {
                        BatchSize = 5,
                        MaxParallelTasks = 1
                    }
                }
            }
        });
        var job = new InitialContractRejectEventDeserializationEventFieldsCatchUpJob(dbFactory.Object, options);
        var parallelBatchJob = new ParallelBatchJob<InitialContractRejectEventDeserializationEventFieldsCatchUpJob>(job, options, new ContractHealthCheck());
        
        // Act
        await parallelBatchJob.StartImport(CancellationToken.None);
        
        // Assert
        await ValidateEventsHasBeenUpdated(_databaseFixture.CreateGraphQlDbContext());
    }

    private async Task ValidateEventsHasBeenUpdated(GraphQlDbContext context)
    {
        const string expected = "{\"data\":\"\",\"to\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]}}";

        await foreach (var contextContractRejectEvent in context.ContractRejectEvents)
        {
            var message = (contextContractRejectEvent.RejectedEvent as RejectedReceive)!.Message;
            message.Should().NotBeNull();
            message.Should().Be(expected);
        }
    }

    private async Task InsertEvents(GraphQlDbContext context)
    {
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_contract_reject_events");
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        var contractAddress = new ContractAddress(1,0);
        await AddContractInitializedEvent(context, contractAddress);
        await AddModule(context, contractAddress);
        await AddContractRejectEvents(context, contractAddress);
    }

    private async Task AddContractInitializedEvent(
        GraphQlDbContext context,
        ContractAddress contractAddress
        )
    {
        var contractInitialized = new ContractInitialized(
            "",
            contractAddress,
            0,
            "",
            null,
            Array.Empty<string>(),
            null);
        var contractEvent = ContractEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithEvent(contractInitialized)
            .Build();
        await context.AddAsync(contractEvent);
        await context.SaveChangesAsync();
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

    private async Task AddContractRejectEvents(
        GraphQlDbContext context,
        ContractAddress contractAddress
        )
    {
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string value = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        
        for (var i = 1UL; i <= 20; i++)
        {
            var contractRejectEvent = new ContractRejectEvent(
                i,
                "",
                1,
                contractAddress,
                new AccountAddress(""),
                new RejectedReceive(
                    1,
                    contractAddress,
                    $"{contractName}.{entrypoint}",
                    value, null
                ),
                ImportSource.NodeImport,
                DateTimeOffset.UtcNow
            );
            await context.AddAsync(contractRejectEvent);
        }

        await context.SaveChangesAsync();
    }
    
}
