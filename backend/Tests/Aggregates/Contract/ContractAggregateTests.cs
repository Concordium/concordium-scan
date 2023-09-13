using System.Collections.Generic;
using System.Text;
using System.Threading;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using FluentAssertions;
using Moq;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;
using ContractEvent = Application.Aggregates.Contract.Entities.ContractEvent;
using ContractInitialized = Concordium.Sdk.Types.ContractInitialized;
using Transferred = Application.Api.GraphQL.Transactions.Transferred;

namespace Tests.Aggregates.Contract;

public sealed class ContractAggregateTests
{
    [Fact]
    public async Task GivenTransferFromContract_WithAccountTo_WhenStoreEvent_TheOneTransfers()
    {
        // Arrange
        const ulong from = 4;
        const ulong amount = 5UL;
        var contractEvents = new List<ContractEvent>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ContractEvent[]>()))
            .Callback<ContractEvent[]>((e) => contractEvents.AddRange(e));
        var transfer = new Transferred(
            amount,
            new Application.Api.GraphQL.ContractAddress(from, 0),
            new AccountAddress(""));
        
        // Act
        await ContractAggregate.StoreEvent(
            ImportSource.NodeImport,
            repository.Object,
            transfer,
            new AccountAddress(""),
            1UL,
            DateTimeOffset.Now,
            "",
            1UL,
            1U);
        
        // Assert
        contractEvents.Count.Should().Be(1);
        var updateEvent = contractEvents[0];
        updateEvent.Event.Should().BeOfType<Transferred>();
        updateEvent.ContractAddressIndex.Should().Be(from);
    }
    
    [Fact]
    public async Task GivenTransferFromContract_WithContractTo_WhenStoreEvent_TheTwoTransfers()
    {
        // Arrange
        const ulong from = 4;
        const ulong to = 2;
        const ulong amount = 5UL;
        var contractEvents = new List<ContractEvent>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ContractEvent[]>()))
            .Callback<ContractEvent[]>((e) => contractEvents.AddRange(e));
        var transfer = new Transferred(
            amount,
            new Application.Api.GraphQL.ContractAddress(from, 0),
            new Application.Api.GraphQL.ContractAddress(to, 0));
        
        // Act
        await ContractAggregate.StoreEvent(
            ImportSource.NodeImport,
            repository.Object,
            transfer,
            new AccountAddress(""),
            1UL,
            DateTimeOffset.Now,
            "",
            1UL,
            1U);
        
        // Assert
        contractEvents.Count.Should().Be(2);
        var updateEvent = contractEvents[0];
        updateEvent.Event.Should().BeOfType<Transferred>();
        updateEvent.ContractAddressIndex.Should().Be(from);
        
        var transferEvent = contractEvents[1];
        transferEvent.Event.Should().BeOfType<Transferred>();
        transferEvent.ContractAddressIndex.Should().Be(to);
    }
    
    [Fact]
    public async Task GivenContractUpdated_WithAccountInstigator_WhenStoreEvent_ThenStoreUpgrade()
    {
        // Arrange
        const int from = 4;
        var messageAsHex = Convert.ToHexString(Encoding.UTF8.GetBytes("Foo"));
        var contractEvents = new List<ContractEvent>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ContractEvent[]>()))
            .Callback<ContractEvent[]>((e) => contractEvents.AddRange(e));
        var contractUpdated = new ContractUpdated(
            new Application.Api.GraphQL.ContractAddress(from, 0),
            new AccountAddress(""),
            5,
            messageAsHex,
            "foo",
            Application.Api.GraphQL.ContractVersion.V0,
            new string[] { "foo", "bar" }
        );
        
        // Act
        await ContractAggregate.StoreEvent(
            ImportSource.NodeImport,
            repository.Object,
            contractUpdated,
            new AccountAddress(""),
            1UL,
            DateTimeOffset.Now,
            "",
            1UL,
            1U);
        
        // Assert
        contractEvents.Count.Should().Be(1);
        var updateEvent = contractEvents[0];
        updateEvent.Event.Should().BeOfType<ContractUpdated>();
        updateEvent.ContractAddressIndex.Should().Be(from);
    }
    
    [Fact]
    public async Task GivenContractUpdated_WithContractInstigator_WhenStoreEvent_ThenStoreUpgradeAndContractCall()
    {
        // Arrange
        const int from = 4;
        const int to = 2;
        var messageAsHex = Convert.ToHexString(Encoding.UTF8.GetBytes("Foo"));
        var contractEvents = new List<ContractEvent>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ContractEvent[]>()))
            .Callback<ContractEvent[]>((e) => contractEvents.AddRange(e));
        var contractUpdated = new ContractUpdated(
            new Application.Api.GraphQL.ContractAddress(from, 0),
            new Application.Api.GraphQL.ContractAddress(to, 0),
            5,
            messageAsHex,
            "foo",
            Application.Api.GraphQL.ContractVersion.V0,
            new string[] { "foo", "bar" }
        );
        
        // Act
        await ContractAggregate.StoreEvent(
            ImportSource.NodeImport,
            repository.Object,
            contractUpdated,
            new AccountAddress(""),
            1UL,
            DateTimeOffset.Now,
            "",
            1UL,
            1U);
        
        // Assert
        contractEvents.Count.Should().Be(2);
        var updateEvent = contractEvents[0];
        updateEvent.Event.Should().BeOfType<ContractUpdated>();
        updateEvent.ContractAddressIndex.Should().Be(from);
        
        var transferEvent = contractEvents[1];
        transferEvent.Event.Should().BeOfType<ContractCall>();
        transferEvent.ContractAddressIndex.Should().Be(to);
    }
    
    [Fact]
    public async Task GivenContractInitialization_WhenNodeImport_ThenStoreContractEventAndModuleLinkAndContract()
    {
        // Arrange
        var contractEvents = new List<ContractEvent>();
        var moduleReferenceContractLinkEvents = new List<ModuleReferenceContractLinkEvent>();
        var contracts = new List<Application.Aggregates.Contract.Entities.Contract>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ContractEvent[]>()))
            .Callback<ContractEvent[]>((e) => contractEvents.AddRange(e));
        repository.Setup(m => m.AddAsync(It.IsAny<ModuleReferenceContractLinkEvent[]>()))
            .Callback<ModuleReferenceContractLinkEvent[]>((e) => moduleReferenceContractLinkEvents.AddRange(e));
        repository.Setup(m => m.AddAsync(It.IsAny<Application.Aggregates.Contract.Entities.Contract[]>()))
            .Callback<Application.Aggregates.Contract.Entities.Contract[]>((e) => contracts.AddRange(e));
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
                new List<Concordium.Sdk.Types.ContractEvent>())
        );
        var client = new Mock<IContractNodeClient>();
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
        
        var aggregate = new ContractAggregate(Mock.Of<IContractRepositoryFactory>(), new ContractAggregateOptions());
        
        // Act
        await aggregate.NodeImport(repository.Object, client.Object, 42);

        // Assert
        contractEvents.Count.Should().Be(1);
        var contractEvent = contractEvents[0];
        contractEvent.Event.Should()
            .BeOfType<Application.Api.GraphQL.Transactions.ContractInitialized>();
        contractEvent.ContractAddressIndex.Should().Be(contractIndex);
        (contractEvent.Event as Application.Api.GraphQL.Transactions.ContractInitialized)!.InitName.Should()
            .Be(initName);
        
        moduleReferenceContractLinkEvents.Count.Should().Be(1);
        var link = moduleReferenceContractLinkEvents[0];
        link.ModuleReference.Should().Be(moduleTo);
        link.ContractAddressIndex.Should().Be(contractIndex);
        link.LinkAction.Should().Be(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added);

        contracts.Count.Should().Be(1);
        var contract = contracts[0];
        contract.ContractAddressIndex.Should().Be(contractIndex);
        contract.Creator.Should().Be(AccountAddress.From(accountAddress));
    }

    [Fact]
    public async Task GivenModuleDeployed_WhenNodeImport_ThenStoreEvent()
    {
        // Arrange
        var moduleReferenceEvents = new List<ModuleReferenceEvent>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ModuleReferenceEvent[]>()))
            .Callback<ModuleReferenceEvent[]>((e) => moduleReferenceEvents.AddRange(e));
        var moduleReference = Convert.ToHexString(new byte[32]);
        var moduleDeployed = new ModuleDeployed(new ModuleReference(moduleReference));
        var client = CreateMockClientFromEffects(moduleDeployed);

        var aggregate = new ContractAggregate(Mock.Of<IContractRepositoryFactory>(), new ContractAggregateOptions());
        
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
        var contractEvents = new List<ContractEvent>();
        var moduleReferenceContractLinkEvents = new List<ModuleReferenceContractLinkEvent>();
        var repository = new Mock<IContractRepository>();
        repository.Setup(m => m.AddAsync(It.IsAny<ContractEvent[]>()))
            .Callback<ContractEvent[]>((e) => contractEvents.AddRange(e));
        repository.Setup(m => m.AddAsync(It.IsAny<ModuleReferenceContractLinkEvent[]>()))
            .Callback<ModuleReferenceContractLinkEvent[]>((e) => moduleReferenceContractLinkEvents.AddRange(e));
        var moduleFrom = Convert.ToHexString(ArrayFilledWith(0, 32));
        var moduleTo = Convert.ToHexString(ArrayFilledWith(1, 32));
        const ulong contractIndex = 1UL;
        var upgraded = new Upgraded(new ContractAddress(contractIndex, 1),
            new ModuleReference(moduleFrom), 
            new ModuleReference(moduleTo)
        );
        var client = CreateMockClientFromEffects(new ContractUpdateIssued(new List<IContractTraceElement>{upgraded}));

        var aggregate = new ContractAggregate(Mock.Of<IContractRepositoryFactory>(), new ContractAggregateOptions());
        
        // Act
        await aggregate.NodeImport(repository.Object, client.Object, 42);

        // Assert
        contractEvents.Count.Should().Be(1);
        var contractUpgraded = (contractEvents[0].Event as Application.Api.GraphQL.Transactions.ContractUpgraded)!;
        contractUpgraded.From.Should().Be(moduleFrom);
        contractUpgraded.To.Should().Be(moduleTo);

        moduleReferenceContractLinkEvents.Count.Should().Be(2);
        
        var to = moduleReferenceContractLinkEvents[0];
        to.ModuleReference.Should().Be(moduleTo);
        to.LinkAction.Should().Be(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added);
        to.ContractAddressIndex.Should().Be(contractIndex);
        
        var from = moduleReferenceContractLinkEvents[1];
        from.ModuleReference.Should().Be(moduleFrom);
        from.LinkAction.Should().Be(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed);
        from.ContractAddressIndex.Should().Be(contractIndex);
    }

    private static byte[] ArrayFilledWith(byte fill, ushort size)
    {
        Span<byte> bytes = stackalloc byte[size];
        bytes.Fill(fill); 
        return bytes.ToArray();
    }
    
    private static Mock<IContractNodeClient> CreateMockClientFromEffects(IAccountTransactionEffects effects)
    {
        var client = new Mock<IContractNodeClient>();
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
