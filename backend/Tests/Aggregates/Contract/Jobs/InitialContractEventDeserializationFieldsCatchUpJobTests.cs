using System.Collections.Generic;
using System.IO;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Extensions;
using Application.Aggregates.Contract.Jobs;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using Application.Configurations;
using Application.Observability;
using Concordium.Sdk.Types;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using static Application.Aggregates.Contract.Jobs.InitialContractEventDeserializationFieldsCatchUpJob;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractAddress = Application.Api.GraphQL.ContractAddress;
using ContractEvent = Application.Aggregates.Contract.Entities.ContractEvent;
using ContractInitialized = Application.Api.GraphQL.Transactions.ContractInitialized;
using ContractVersion = Application.Api.GraphQL.ContractVersion;
using Transferred = Application.Api.GraphQL.Transactions.Transferred;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class InitialContractEventDeserializationFieldsCatchUpJobTests
{
    private readonly DatabaseFixture _databaseFixture;

    public InitialContractEventDeserializationFieldsCatchUpJobTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }

    #region MainExecutionFlow
    
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
            Jobs = new Dictionary<string, JobOptions>
            {
                {
                    InitialContractRejectEventDeserializationFieldsCatchUpJob.JobName,
                    new JobOptions
                    {
                        BatchSize = 3
                    }
                }
            }
        });
        var job = new InitialContractEventDeserializationFieldsCatchUpJob(dbFactory.Object, options);
        var parallelBatchJob = new ParallelBatchJob<InitialContractEventDeserializationFieldsCatchUpJob>(job, options, new JobHealthCheck());
        
        // Act
        await parallelBatchJob.StartImport(CancellationToken.None);
        
        // Assert
        await ValidateEventsHasBeenUpdated(_databaseFixture.CreateGraphQlDbContext());
    }

    private static async Task ValidateEventsHasBeenUpdated(GraphQlDbContext context)
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

    private static async Task InsertEvents(GraphQlDbContext context)
    {
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        var contractAddress = new ContractAddress(1,0);
        await AddModule(context, contractAddress);
        await AddContractEvents(context, contractAddress);
    }

    private static async Task AddModule(
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
            ModuleSchemaVersion.Undefined,
            ImportSource.NodeImport,
            DateTimeOffset.UtcNow
        );
        await context.AddAsync(moduleReferenceContractLinkEvent);
        await context.AddAsync(moduleReferenceEvent);
        await context.SaveChangesAsync();
    }

    private static async Task AddContractEvents(
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
    
    #endregion
    
    [Fact]
    public async Task GivenModuleEventsInMemory_WhenGetModuleReferenceEventAtAsync_ThenReturnLatest()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        const ulong blockHeight = 5UL;
        const ulong transactionIndex = 5UL;
        const uint eventIndex = 5U;
        const string moduleRef = "foo";
        const string moduleRefOther = "bar";
        var contract = new ContractAddress(4, 2);
        // Events before
        var linkAddFirst = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 3)
            .WithTransactionIndex(transactionIndex - 3)
            .WithEventIndex(eventIndex - 3)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Latest added event
        var linkAddSecond = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 2)
            .WithTransactionIndex(transactionIndex - 2)
            .WithEventIndex(eventIndex - 2)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var linkRemoveFirst = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 2)
            .WithTransactionIndex(transactionIndex - 2)
            .WithEventIndex(eventIndex - 2)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed)
            .Build();
        // Event closer to limit but another contract address
        var linkAddOther = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(new ContractAddress(1,1))
            .WithBlockHeight(blockHeight - 1)
            .WithTransactionIndex(transactionIndex - 1)
            .WithEventIndex(eventIndex - 1)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Events after
        var linkRemoveSecond = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight + 3)
            .WithTransactionIndex(transactionIndex + 3)
            .WithEventIndex(eventIndex + 3)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed)
            .Build();        
        var linkAddThird = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight + 3)
            .WithTransactionIndex(transactionIndex + 3)
            .WithEventIndex(eventIndex + 3)      
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var module = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRef)
            .Build();
        var moduleOther = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRefOther)
            .Build();
        var context = _databaseFixture.CreateGraphQlDbContext();
        await context.AddRangeAsync(linkAddFirst, linkAddSecond, linkAddThird, linkRemoveFirst, linkRemoveSecond, linkAddOther);
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var moduleReadonlyRepository = InMemoryModuleRepository.Create(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventAtSameTransactionIndexInMemory_WithDifferentEventIndex_WhenGetModuleReferenceEventAtAsync_ThenReturnLatest()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        var context = _databaseFixture.CreateGraphQlDbContext();
        const ulong transactionIndex = 1UL;
        const uint eventIndex = 2U;
        const string moduleRef = "foo";
        const string moduleRefOther = "bar";
        var contract = new ContractAddress(4, 2);
        // Events before
        var before1 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex - 1UL)
            .WithEventIndex(eventIndex)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var before2 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex)
            .WithEventIndex(eventIndex - 1)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Expected
        var expected = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex)
            .WithEventIndex(eventIndex)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // After
        var after1 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex)
            .WithEventIndex(eventIndex + 1)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var after2 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex)
            .WithEventIndex(eventIndex + 2)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Add modules
        var module = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRef)
            .Build();
        var moduleOther = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRefOther)
            .Build();
        await context.AddRangeAsync(before1, before2, expected, after1, after2);
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var moduleReadonlyRepository = InMemoryModuleRepository.Create(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, expected.BlockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }    

    [Fact]
    public async Task
        GivenModuleEventAtSameBlockHeightInMemory_WithDifferentTransactionIndex_WhenGetModuleReferenceEventAtAsync_ThenReturnLatest()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        var context = _databaseFixture.CreateGraphQlDbContext();
        const ulong blockHeight = 11UL;
        const ulong transactionIndex = 1UL;
        const string moduleRef = "foo";
        const string moduleRefOther = "bar";
        var contract = new ContractAddress(4, 2);
        // Events before
        var before1 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 1UL)
            .WithTransactionIndex(transactionIndex)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var before2 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight)
            .WithTransactionIndex(transactionIndex - 1UL)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Expected
        var expected = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight)
            .WithTransactionIndex(transactionIndex)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // After
        var after1 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight)
            .WithTransactionIndex(transactionIndex + 1UL)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var after2 = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight)
            .WithTransactionIndex(transactionIndex + 2UL)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Add modules
        var module = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRef)
            .Build();
        var moduleOther = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRefOther)
            .Build();
        await context.AddRangeAsync(before1, before2, expected, after1, after2);
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var moduleReadonlyRepository = InMemoryModuleRepository.Create(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, expected.EventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventWithHigherAbsoluteTransactionIndexInMemory_WhenGetModuleReferenceEventAtAsync_ThenReturnLatest()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        var context = _databaseFixture.CreateGraphQlDbContext();
        const ulong blockHeight = 11UL;
        const ulong transactionIndex = 1UL;
        const string moduleRef = "foo";
        const string moduleRefOther = "bar";
        var contract = new ContractAddress(4, 2);
        // Events before
        var before = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 2UL)
            .WithTransactionIndex(transactionIndex)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Expected
        // Transaction index above but block height below
        var expected = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 1UL)
            .WithTransactionIndex(transactionIndex + 1UL)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // After
        var after = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight)
            .WithTransactionIndex(transactionIndex + 1UL)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Add modules
        var module = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRef)
            .Build();
        var moduleOther = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRefOther)
            .Build();
        await context.AddRangeAsync(before, expected, after);
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();

        var moduleReadonlyRepository = InMemoryModuleRepository.Create(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, expected.EventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventWithHigherAbsoluteEventIndexInMemory_WhenGetModuleReferenceEventAtAsync_ThenReturnLatest()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        var context = _databaseFixture.CreateGraphQlDbContext();
        const ulong transactionIndex = 3UL;
        const uint eventIndex = 3;
        const string moduleRef = "foo";
        const string moduleRefOther = "bar";
        var contract = new ContractAddress(4, 2);
        // Before
        var before = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex - 2UL)
            .WithEventIndex(eventIndex - 1)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Expected
        // Event index above but transaction index below
        var expected = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex - 1UL)
            .WithEventIndex(eventIndex + 1)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // After
        var after = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithTransactionIndex(transactionIndex)
            .WithEventIndex(eventIndex + 1)
            .WithModuleReference(moduleRefOther)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        // Add modules
        var module = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRef)
            .Build();
        var moduleOther = ModuleReferenceEventBuilder
            .Create()
            .WithModuleReference(moduleRefOther)
            .Build();
        await context.AddRangeAsync(before, expected, after);
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        var moduleReadonlyRepository = InMemoryModuleRepository.Create(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, expected.BlockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task GivenContractInitializedEventInMemory_WhenGetReadonlyContractInitializedEventAsync_ThenReturnCorrect()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_events");
        ContractExtensions.AddDapperTypeHandlers();
        var context = _databaseFixture.CreateGraphQlDbContext();
        var oneContract = new ContractAddress(1, 0);
        const string oneContractName = "init_foo";
        const string otherContractName = "init_bar";
        var otherContract = new ContractAddress(2,0);

        var first = ContractEventBuilder.Create()
            .WithContractAddress(oneContract)
            .WithBlockHeight(1)
            .WithEvent(new ContractInitialized("", oneContract, 10, oneContractName, ContractVersion.V0, Array.Empty<string>()))
            .Build();
        var second = ContractEventBuilder.Create()
            .WithContractAddress(oneContract)
            .WithBlockHeight(2)
            .WithEvent(new Transferred(2, oneContract, new AccountAddress("")))
            .Build();
        var third = ContractEventBuilder.Create()
            .WithContractAddress(otherContract)
            .WithBlockHeight(3)
            .WithEvent(new ContractInitialized("", otherContract, 10, otherContractName, ContractVersion.V0, Array.Empty<string>()))
            .Build();
        await context.AddRangeAsync(first, second, third);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        var repository = InMemoryContractRepository.Create(context);

        // Act
        var initialized = await repository.GetReadonlyContractInitializedEventAsync(oneContract);

        // Assert
        initialized.InitName.Should().Be(oneContractName);
    }
}
