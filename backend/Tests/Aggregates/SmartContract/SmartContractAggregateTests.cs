using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.SmartContract;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using FluentAssertions;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractInitialized = Concordium.Sdk.Types.ContractInitialized;

namespace Tests.Aggregates.SmartContract;

public sealed class SmartContractAggregateTests
{
    [Fact]
    public async Task GivenSomeRows_WhenGetLastReadBlockHeight_ThenReturnLatest()
    {
        // Arrange
        const ulong lastHeight = 9UL;
        var repository = new TestSmartContractRepository();
        repository.SmartContractAggregateImportStates.Add(new SmartContractReadHeight(3));
        repository.SmartContractAggregateImportStates.Add(new SmartContractReadHeight(7));
        repository.SmartContractAggregateImportStates.Add(new SmartContractReadHeight(1));
        repository.SmartContractAggregateImportStates.Add(new SmartContractReadHeight(lastHeight));
        var testMockDbFactory = new TestSmartContractRepositoryFactory(repository);
        var aggregate = new SmartContractAggregate(testMockDbFactory);

        // Act
        var lastReadBlockHeight = await aggregate.GetLastReadBlockHeight();

        // Assert
        lastReadBlockHeight.Should().Be(lastHeight);
    }
    
    [Fact]
    public async Task GivenNoRows_WhenGetLastReadBlockHeight_ThenReturnZero()
    {
        // Arrange
        var repository = new TestSmartContractRepository();
        var testMockDbFactory = new TestSmartContractRepositoryFactory(repository);
        var aggregate = new SmartContractAggregate(testMockDbFactory);

        // Act
        var lastReadBlockHeight = await aggregate.GetLastReadBlockHeight();

        // Assert
        lastReadBlockHeight.Should().Be(0);
    }
    
    [Fact]
    public async Task GivenContractInitialization_WhenNodeImport_ThenStoreSmartContractEventAndModuleLinkAndSmartContract()
    {
        // Arrange
        var repository = new TestSmartContractRepository();
        const int contractIndex = 5;
        const string initName = "init_foo";
        var accountAddress = AccountAddressHelper.CreateOneFilledWith(1);
        _ = ContractName.TryParse(initName, out var contractNameParse);
        var moduleTo = Convert.ToHexString(new byte[32]);
        var contractInitialized = new ContractInitialized(
            new ContractInitializedEvent(
                ContractVersion.V0,
                new ModuleReference(moduleTo),
                new ContractAddress(contractIndex, 0),
                CcdAmount.Zero, 
                contractNameParse.ContractName!,
                new List<ContractEvent>())
        );
        var client = new Mock<ISmartContractNodeClient>();
        var transactionDetails = new AccountTransactionDetailsBuilder(contractInitialized)
            .WithSender(accountAddress)
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
        
        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>());
        
        // Act
        await aggregate.NodeImport(repository, client.Object, 42);

        // Assert
        repository.SmartContractEvents.Count.Should().Be(1);
        var contractEvent = repository.SmartContractEvents[0];
        contractEvent.Event.Should()
            .BeOfType<Application.Api.GraphQL.Transactions.ContractInitialized>();
        contractEvent.ContractAddressIndex.Should().Be(contractIndex);
        (contractEvent.Event as Application.Api.GraphQL.Transactions.ContractInitialized)!.InitName.Should()
            .Be(initName);
        
        repository.ModuleReferenceSmartContractLinkEvents.Count.Should().Be(1);
        var link = repository.ModuleReferenceSmartContractLinkEvents[0];
        link.ModuleReference.Should().Be(moduleTo);
        link.ContractAddressIndex.Should().Be(contractIndex);

        repository.SmartContracts.Count.Should().Be(1);
        var smartContract = repository.SmartContracts[0];
        smartContract.ContractAddressIndex.Should().Be(contractIndex);
        smartContract.Creator.Should().Be(AccountAddress.From(accountAddress));
    }

    [Fact]
    public async Task GivenModuleDeployed_WhenNodeImport_ThenStoreEvent()
    {
        // Arrange
        var repository = new TestSmartContractRepository();
        var moduleReference = Convert.ToHexString(new byte[32]);
        var moduleDeployed = new ModuleDeployed(new ModuleReference(moduleReference));
        var client = CreateMockClientFromEffects(moduleDeployed);

        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>());
        
        // Act
        await aggregate.NodeImport(repository, client.Object, 42);

        // Assert
        repository.ModuleReferenceEvents.Count.Should().Be(1);
        var repoEvent = repository.ModuleReferenceEvents[0];
        repoEvent.ModuleReference.Should().Be(moduleReference);
    }

    [Fact]
    public async Task GivenContractUpgraded_WhenNodeImport_ThenStoreEventAndModuleLink()
    {
        // Arrange
        var repository = new TestSmartContractRepository();
        var moduleReference = Convert.ToHexString(new byte[32]);
        var moduleFrom = Convert.ToHexString(ArrayFilledWith(0, 32));
        var moduleTo = Convert.ToHexString(ArrayFilledWith(1, 32));
        const ulong contractIndex = 1UL;
        var upgraded = new Upgraded(new ContractAddress(contractIndex, 1),
            new ModuleReference(moduleFrom), 
            new ModuleReference(moduleTo)
        );
        var client = CreateMockClientFromEffects(new ContractUpdateIssued(new List<IContractTraceElement>{upgraded}));

        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>());
        
        // Act
        await aggregate.NodeImport(repository, client.Object, 42);

        // Assert
        repository.SmartContractEvents.Count.Should().Be(1);
        var contractUpgraded = (repository.SmartContractEvents[0].Event as Application.Api.GraphQL.Transactions.ContractUpgraded)!;
        contractUpgraded.From.Should().Be(moduleFrom);
        contractUpgraded.To.Should().Be(moduleTo);

        repository.ModuleReferenceSmartContractLinkEvents.Count.Should().Be(1);
        var link = repository.ModuleReferenceSmartContractLinkEvents[0];
        link.ModuleReference.Should().Be(moduleTo);
        link.ContractAddressIndex.Should().Be(contractIndex);
    }

    private static byte[] ArrayFilledWith(byte fill, ushort size)
    {
        Span<byte> bytes = stackalloc byte[size];
        bytes.Fill(fill); 
        return bytes.ToArray();
    }
    
    private static Mock<ISmartContractNodeClient> CreateMockClientFromEffects(IAccountTransactionEffects effects)
    {
        var client = new Mock<ISmartContractNodeClient>();
        var transactionDetails = new AccountTransactionDetailsBuilder(effects)
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
        return client;
    }
}