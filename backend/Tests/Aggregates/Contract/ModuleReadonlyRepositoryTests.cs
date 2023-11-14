using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using FluentAssertions;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.Contract;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class ModuleReadonlyRepositoryTests
{
    private readonly DatabaseFixture _databaseFixture;

    public ModuleReadonlyRepositoryTests(DatabaseFixture databaseFixture)
    {
        _databaseFixture = databaseFixture;
    }
    
    [Fact]
    public async Task GivenModuleEventsInDatabase_WhenGetModuleReferenceEventAtAsync_ThenReturnLatest()
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

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
        [Fact]
    public async Task
        GivenModuleEventAtSameTransactionIndexInDatabase_WithDifferentEventIndex_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromDatabase()
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

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, expected.BlockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }    

    [Fact]
    public async Task
        GivenModuleEventAtSameBlockHeightInDatabase_WithDifferentTransactionIndex_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromDatabase()
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

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, expected.EventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventWithHigherAbsoluteTransactionIndexInDatabase_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromDatabase()
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

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, expected.EventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventWithHigherAbsoluteEventIndexInDatabase_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromDatabase()
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
        
        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, expected.BlockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }    
    
    [Fact]
    public async Task
        GivenModuleEventAtSameTransactionIndexInChangeTracker_WithDifferentEventIndex_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromChangeTracker()
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
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Add but not yet committed
        await context.AddRangeAsync(before1, before2, expected, after1, after2);

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, expected.BlockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }    

    [Fact]
    public async Task
        GivenModuleEventAtSameBlockHeightInChangeTracker_WithDifferentTransactionIndex_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromChangeTracker()
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
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Add but not yet committed
        await context.AddRangeAsync(before1, before2, expected, after1, after2);

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, expected.EventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventWithHigherAbsoluteTransactionIndexInChangeTracker_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromChangeTracker()
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
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Add but not yet committed
        await context.AddRangeAsync(before, expected, after);

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, expected.EventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task
        GivenModuleEventWithHigherAbsoluteEventIndexInChangeTracker_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromChangeTracker()
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
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Add but not yet committed
        await context.AddRangeAsync(before, expected, after);

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, expected.BlockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }
    
    [Fact]
    public async Task GivenModuleEventsInChangeTracker_WhenGetModuleReferenceEventAtAsync_ThenReturnLatestFromChangeTracker()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_module_reference_events");
        await DatabaseFixture.TruncateTables("graphql_module_reference_contract_link_events");
        var context = _databaseFixture.CreateGraphQlDbContext();
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
        // Latest added event in database
        var linkAddSecond = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 2)
            .WithTransactionIndex(transactionIndex - 2)
            .WithEventIndex(eventIndex - 2)
            .WithModuleReference(moduleRefOther)
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
        // Events after
        var linkRemoveSecond = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight + 3)
            .WithTransactionIndex(transactionIndex + 3)
            .WithEventIndex(eventIndex + 3)
            .WithModuleReference(moduleRefOther)
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
        await context.AddRangeAsync(linkAddFirst, linkAddSecond, linkAddThird, linkRemoveFirst, linkRemoveSecond);
        await context.AddRangeAsync(module, moduleOther);
        await context.SaveChangesAsync();
        context.ChangeTracker.Clear();
        
        // Event closer to limit
        var linkAddOther = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contract)
            .WithBlockHeight(blockHeight - 1)
            .WithTransactionIndex(transactionIndex - 1)
            .WithEventIndex(eventIndex - 1)
            .WithModuleReference(moduleRef)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();

        // Add but not yet committed
        await context.AddAsync(linkAddOther);

        var moduleReadonlyRepository = new ModuleReadonlyRepository(context);

        // Act
        var actualModule = await moduleReadonlyRepository.GetModuleReferenceEventAtAsync(contract, blockHeight, transactionIndex, eventIndex);
        
        // Assert
        actualModule.ModuleReference.Should().Be(moduleRef);
    }    
}
