using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.SmartContract;
using Application.Aggregates.SmartContract.Configurations;
using Application.Aggregates.SmartContract.Entities;
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
    public void GivenNonContinuousRange_ThenReturnInternals()
    {
        // Arrange
        var read = new List<ulong> { 0, 2, 3, 4, 7, 8, 10, 12, 14, 15, 16, 20, 21, 22 };
        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>(),
            new SmartContractAggregateOptions());

        // Act
        var intervals = SmartContractAggregate.PrettifyToRanges(read);

        // Assert
        intervals.Should().BeEquivalentTo(new List<(ulong, ulong)> { (0, 0), (2, 4), (7,8), (10,10), (12,12), (14,16), (20,22) });
    }
    
    [Fact]
    public async Task GivenContractInitialization_WhenNodeImport_ThenStoreSmartContractEventAndModuleLinkAndSmartContract()
    {
        // Arrange
        var smartContractEvents = new List<SmartContractEvent>();
        var moduleReferenceSmartContractLinkEvents = new List<ModuleReferenceSmartContractLinkEvent>();
        var smartContracts = new List<Application.Aggregates.SmartContract.Entities.SmartContract>();
        var repository = new Mock<ISmartContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<SmartContractEvent[]>()))
            .Callback<SmartContractEvent[]>((e) => smartContractEvents.AddRange(e));
        repository.Setup(m => m.AddAsync(It.IsAny<ModuleReferenceSmartContractLinkEvent[]>()))
            .Callback<ModuleReferenceSmartContractLinkEvent[]>((e) => moduleReferenceSmartContractLinkEvents.AddRange(e));
        repository.Setup(m => m.AddAsync(It.IsAny<Application.Aggregates.SmartContract.Entities.SmartContract[]>()))
            .Callback<Application.Aggregates.SmartContract.Entities.SmartContract[]>((e) => smartContracts.AddRange(e));
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
        
        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>(), new SmartContractAggregateOptions());
        
        // Act
        await aggregate.NodeImport(repository.Object, client.Object, 42);

        // Assert
        smartContractEvents.Count.Should().Be(1);
        var contractEvent = smartContractEvents[0];
        contractEvent.Event.Should()
            .BeOfType<Application.Api.GraphQL.Transactions.ContractInitialized>();
        contractEvent.ContractAddressIndex.Should().Be(contractIndex);
        (contractEvent.Event as Application.Api.GraphQL.Transactions.ContractInitialized)!.InitName.Should()
            .Be(initName);
        
        moduleReferenceSmartContractLinkEvents.Count.Should().Be(1);
        var link = moduleReferenceSmartContractLinkEvents[0];
        link.ModuleReference.Should().Be(moduleTo);
        link.ContractAddressIndex.Should().Be(contractIndex);

        smartContracts.Count.Should().Be(1);
        var smartContract = smartContracts[0];
        smartContract.ContractAddressIndex.Should().Be(contractIndex);
        smartContract.Creator.Should().Be(AccountAddress.From(accountAddress));
    }

    [Fact]
    public async Task GivenModuleDeployed_WhenNodeImport_ThenStoreEvent()
    {
        // Arrange
        var moduleReferenceEvents = new List<ModuleReferenceEvent>();
        var repository = new Mock<ISmartContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ModuleReferenceEvent[]>()))
            .Callback<ModuleReferenceEvent[]>((e) => moduleReferenceEvents.AddRange(e));
        var moduleReference = Convert.ToHexString(new byte[32]);
        var moduleDeployed = new ModuleDeployed(new ModuleReference(moduleReference));
        var client = CreateMockClientFromEffects(moduleDeployed);

        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>(), new SmartContractAggregateOptions());
        
        // Act
        await aggregate.NodeImport(repository.Object, client.Object, 42);

        // Assert
        moduleReferenceEvents.Count.Should().Be(1);
        var repoEvent = moduleReferenceEvents[0];
        repoEvent.ModuleReference.Should().Be(moduleReference);
    }

    [Fact]
    public async Task GivenContractUpgraded_WhenNodeImport_ThenStoreEventAndModuleLink()
    {
        // Arrange
        var smartContractEvents = new List<SmartContractEvent>();
        var moduleReferenceSmartContractLinkEvents = new List<ModuleReferenceSmartContractLinkEvent>();
        var repository = new Mock<ISmartContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<SmartContractEvent[]>()))
            .Callback<SmartContractEvent[]>((e) => smartContractEvents.AddRange(e));
        repository.Setup(m => m.AddAsync(It.IsAny<ModuleReferenceSmartContractLinkEvent[]>()))
            .Callback<ModuleReferenceSmartContractLinkEvent[]>((e) => moduleReferenceSmartContractLinkEvents.AddRange(e));
        var moduleFrom = Convert.ToHexString(ArrayFilledWith(0, 32));
        var moduleTo = Convert.ToHexString(ArrayFilledWith(1, 32));
        const ulong contractIndex = 1UL;
        var upgraded = new Upgraded(new ContractAddress(contractIndex, 1),
            new ModuleReference(moduleFrom), 
            new ModuleReference(moduleTo)
        );
        var client = CreateMockClientFromEffects(new ContractUpdateIssued(new List<IContractTraceElement>{upgraded}));

        var aggregate = new SmartContractAggregate(Mock.Of<ISmartContractRepositoryFactory>(), new SmartContractAggregateOptions());
        
        // Act
        await aggregate.NodeImport(repository.Object, client.Object, 42);

        // Assert
        smartContractEvents.Count.Should().Be(1);
        var contractUpgraded = (smartContractEvents[0].Event as Application.Api.GraphQL.Transactions.ContractUpgraded)!;
        contractUpgraded.From.Should().Be(moduleFrom);
        contractUpgraded.To.Should().Be(moduleTo);

        moduleReferenceSmartContractLinkEvents.Count.Should().Be(1);
        var link = moduleReferenceSmartContractLinkEvents[0];
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