using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.SmartContract;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Moq;
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.SmartContract;

public sealed class SmartContractAggregateTests
{
    [Fact]
    public async Task GivenSomeRows_WhenGetLastReadBlockHeight_ThenReturnLatest()
    {
        // Arrange
        const ulong lastHeight = 9UL;
        var repository = new TestRepository();
        repository.SmartContractAggregateImportStates = new List<SmartContractReadHeight>
        {
            new(3),
            new(7),
            new(1),
            new(lastHeight)
        };
        var testMockDbFactory = new TestMockDbFactory(repository);
        var aggregate = new SmartContractAggregate(testMockDbFactory, Mock.Of<ISmartContractNodeClient>());

        // Act
        var lastReadBlockHeight = await aggregate.GetLastReadBlockHeight();

        // Assert
        lastReadBlockHeight.Should().Be(lastHeight);
    }
    
    [Fact]
    public async Task GivenNoRows_WhenGetLastReadBlockHeight_ThenReturnZero()
    {
        // Arrange
        var repository = new TestRepository();
        var testMockDbFactory = new TestMockDbFactory(repository);
        var aggregate = new SmartContractAggregate(testMockDbFactory, Mock.Of<ISmartContractNodeClient>());

        // Act
        var lastReadBlockHeight = await aggregate.GetLastReadBlockHeight();

        // Assert
        lastReadBlockHeight.Should().Be(0);
    }
    
    [Fact]
    public async Task GivenContractInitialization_WhenGetTransaction_ThenStoreEvent()
    {
        // Arrange
        var repository = new TestRepository();
        var client = new Mock<ISmartContractNodeClient>();
        const int contractIndex = 5;
        const string initName = "init_foo";
        _ = ContractName.TryParse(initName, out var contractNameParse);
        var contractInitialized = new ContractInitialized(
            new ContractInitializedEvent(
                ContractVersion.V0,
                new ModuleReference(Convert.ToHexString(new byte[32])),
                new ContractAddress(contractIndex, 0),
                CcdAmount.Zero, 
                contractNameParse.ContractName!,
                new List<ContractEvent>())
        );
        var transactionDetails = new AccountTransactionDetailsBuilder(contractInitialized)
            .Build();
        var blockItemSummary = new BlockItemSummaryBuilder(transactionDetails)
            .Build();
        var blockHash = BlockHash.From(new byte[32]);
        var asyncEnumerable = new List<BlockItemSummary>{blockItemSummary}.ToAsyncEnumerable();
        var queryResponse = new QueryResponse<IAsyncEnumerable<BlockItemSummary>>(blockHash, asyncEnumerable);

        var blockInfo = new BlockInfoBuilder()
            .WithBlockHash(blockHash)
            .Build();
        var queryResponseBlockInfo = new QueryResponse<BlockInfo>(blockHash, blockInfo);
        
        client.Setup(c => c.GetBlockTransactionEvents(It.IsAny<IBlockHashInput>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(queryResponse));
        client.Setup(c => c.GetBlockInfoAsync(It.IsAny<IBlockHashInput>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(queryResponseBlockInfo));

        var aggregate = new SmartContractAggregate(
            Mock.Of<ISmartContractRepositoryFactory>(),
            client.Object);
        
        // Act
        await aggregate.GetTransactionEvents(
            repository,
            Mock.Of<IBlockHashInput>()
        );

        // Assert
        repository.SmartContractEvents.Count.Should().Be(1);
        var contractEvent = repository.SmartContractEvents[0];
        contractEvent.Event.Should()
            .BeOfType<Application.Api.GraphQL.Transactions.ContractInitialized>();
        contractEvent.ContractAddressIndex.Should().Be(contractIndex);
        (contractEvent.Event as Application.Api.GraphQL.Transactions.ContractInitialized)!.InitName.Should()
            .Be(initName);
    }
}


public sealed class TestRepository : ISmartContractRepository
{
    internal IList<SmartContractReadHeight> SmartContractAggregateImportStates = new List<SmartContractReadHeight>();
    internal IList<SmartContractEvent> SmartContractEvents = new List<SmartContractEvent>();
    internal IList<ModuleReferenceEvent> ModuleReferenceEvents = new List<ModuleReferenceEvent>();
    internal IList<ModuleReferenceSmartContractLinkEvent> ModuleReferenceSmartContractLinkEvents = new List<ModuleReferenceSmartContractLinkEvent>();

    public IQueryable<T> GetReadOnlyQueryable<T>() where T : class
    {
        if (typeof(T) == typeof(SmartContractReadHeight))
        {
            return SmartContractAggregateImportStates.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(SmartContractEvent))
        {
            return SmartContractEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(ModuleReferenceEvent))
        {
            return ModuleReferenceEvents.Cast<T>().AsQueryable();
        }
        if (typeof(T) == typeof(ModuleReferenceSmartContractLinkEvent))
        {
            return ModuleReferenceSmartContractLinkEvents.Cast<T>().AsQueryable();
        }
        throw new NotImplementedException($"Not implemented for type: {typeof(T)}");
    }

    public ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class
    {
        if (typeof(T) == typeof(SmartContractReadHeight))
        {
            SmartContractAggregateImportStates.Add((entity as SmartContractReadHeight)!);
            return new ValueTask<EntityEntry<T>>();
        }
        if (typeof(T) == typeof(SmartContractEvent))
        {
            SmartContractEvents.Add((entity as SmartContractEvent)!);
            return new ValueTask<EntityEntry<T>>();
        }
        if (typeof(T) == typeof(ModuleReferenceEvent))
        {
            ModuleReferenceEvents.Add((entity as ModuleReferenceEvent)!);
            return new ValueTask<EntityEntry<T>>();
        }
        if (typeof(T) == typeof(ModuleReferenceSmartContractLinkEvent))
        {
            ModuleReferenceSmartContractLinkEvents.Add((entity as ModuleReferenceSmartContractLinkEvent)!);
            return new ValueTask<EntityEntry<T>>();
        }
        throw new NotImplementedException($"Not implemented for type: {typeof(T)}");
    }

    public Task SaveChangesAsync(CancellationToken token)
    {
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }
}

internal class TestMockDbFactory : ISmartContractRepositoryFactory
{
    private readonly TestRepository _repository;

    public TestMockDbFactory(TestRepository repository)
    {
        _repository = repository;
    }
    public Task<ISmartContractRepository> CreateAsync()
    {
        return Task.FromResult<ISmartContractRepository>(_repository);
    }
}
