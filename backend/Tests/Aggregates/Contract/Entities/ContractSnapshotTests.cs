using System.Collections.Generic;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.Contract.Entities;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public sealed class ContractSnapshotTests
{
    private readonly DatabaseFixture _fixture;
    
    public ContractSnapshotTests(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task GivenExistingSnapshot_WhenImport_ThenUpdate()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_snapshot", "graphql_module_reference_contract_link_events", "graphql_contract_events", "graphql_contracts");
        var contractAddress = new ContractAddress(0, 0);
        var other = new ContractAddress(2, 1);
        var someAccount = new AccountAddress("");
        const string expectedModuleReference = "foobar";
        const string expectedContractName = "foo";
        var existingSnapshot = new ContractSnapshot(0, contractAddress, expectedContractName, expectedModuleReference, 10, ImportSource.DatabaseImport);
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            await context.AddAsync(existingSnapshot);
            await context.SaveChangesAsync();
        }
        var contractEvents = new List<ContractEvent>{
            // Subtract Transferred
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(2)
                .WithEvent(new Transferred(2, contractAddress, someAccount)).Build(),
            // Add Contract Updated
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(3)
                .WithEvent(new ContractUpdated(new ContractAddress(1,0), other, 42, "", "", ContractVersion.V0,  Array.Empty<string>())).Build(),
            // Subtract Contract Call
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(4)
                .WithEvent(new ContractCall(new ContractUpdated(other, contractAddress, 8, "", "", ContractVersion.V0,  Array.Empty<string>()))).Build(), 
        };
        var dbContextFactory = _fixture.CreateDbContractFactoryMock().Object;
        var contractRepository = await ContractRepository.Create(dbContextFactory);
        var repositoryFactory = new RepositoryFactory(dbContextFactory);
        await contractRepository.AddRangeAsync(contractEvents);
        
        // Act
        await ContractSnapshot.ImportContractSnapshot(repositoryFactory, contractRepository, ImportSource.DatabaseImport);
        await contractRepository.CommitAsync();

        // Assert
        await using var assertContext = _fixture.CreateGraphQlDbContext();
        var contractSnapshots = await assertContext.ContractSnapshots.OrderByDescending(s => s.BlockHeight).ToListAsync();
        contractSnapshots.Count.Should().Be(2);
        var contractSnapshot = contractSnapshots.First();
        contractSnapshot.ContractAddressIndex.Should().Be(contractAddress.Index);
        contractSnapshot.Amount.Should().Be(42);
        contractSnapshot.ModuleReference.Should().Be(expectedModuleReference);
        contractSnapshot.ContractName.Should().Be(expectedContractName);
    }

    [Fact]
    public async Task GivenExistingSnapshot_WhenUpdateModule_ThenUpdateModuleReference()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_snapshot", "graphql_module_reference_contract_link_events", "graphql_contract_events", "graphql_contracts");
        var dbContextFactory = _fixture.CreateDbContractFactoryMock().Object;
        var contractRepository = await ContractRepository.Create(dbContextFactory);
        var repositoryFactory = new RepositoryFactory(dbContextFactory);
        var contractAddress = new ContractAddress(0, 0);
        const string expectedModuleReference = "foobar";
        const string expectedContractName = "foo";
        const ulong expectedAmount = 10;
        var existingSnapshot = new ContractSnapshot(0, contractAddress, expectedContractName, "before", expectedAmount, ImportSource.DatabaseImport);
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            await context.AddAsync(existingSnapshot);
            await context.SaveChangesAsync();
        }
        var link = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithModuleReference(expectedModuleReference)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var contractEvent = ContractEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithEvent(new ContractUpgraded(contractAddress, "before", expectedModuleReference)).Build();
        await contractRepository.AddAsync(contractEvent);
        await contractRepository.AddAsync(link);
        
        // Act
        await ContractSnapshot.ImportContractSnapshot(repositoryFactory, contractRepository, ImportSource.DatabaseImport);
        await contractRepository.CommitAsync();

        // Assert
        await using var assertContext = _fixture.CreateGraphQlDbContext();
        var contractSnapshots = await assertContext.ContractSnapshots.OrderByDescending(s => s.BlockHeight).ToListAsync();
        contractSnapshots.Count.Should().Be(2);
        var contractSnapshot = contractSnapshots.First();
        contractSnapshot.ContractAddressIndex.Should().Be(contractAddress.Index);
        contractSnapshot.Amount.Should().Be(expectedAmount);
        contractSnapshot.ModuleReference.Should().Be(expectedModuleReference);
        contractSnapshot.ContractName.Should().Be(expectedContractName);   
    }
    
    [Fact]
    public async Task GivenMultipleModuleLinkEvents_WhenHavingInitializationEvent_ThenUseLatestModuleLink()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_snapshot", "graphql_module_reference_contract_link_events", "graphql_contract_events", "graphql_contracts");
        var dbContextFactory = _fixture.CreateDbContractFactoryMock().Object;
        var contractRepository = await ContractRepository.Create(dbContextFactory);
        var repositoryFactory = new RepositoryFactory(dbContextFactory);
        var contractAddress = new ContractAddress(0, 0);
        const string expectedModuleReference = "foobar";
        const string expectedContractName = "foo";
        const ulong expectedAmount = 10;
        var firstLink = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithBlockHeight(1)
            .WithTransactionIndex(1)
            .WithEventIndex(1)
            .WithModuleReference("first")
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var secondLink = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithBlockHeight(1)
            .WithTransactionIndex(1)
            .WithEventIndex(2)
            .WithModuleReference(expectedModuleReference)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var contractEvent = ContractEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithEvent(new ContractInitialized("", new ContractAddress(1,0), expectedAmount, $"init_{expectedContractName}", ContractVersion.V0, Array.Empty<string>())).Build();
        await contractRepository.AddAsync(firstLink, secondLink);
        await contractRepository.AddAsync(contractEvent);
        
        // Act
        await ContractSnapshot.ImportContractSnapshot(repositoryFactory, contractRepository, ImportSource.DatabaseImport);
        await contractRepository.CommitAsync();

        // Assert
        await using var assertContext = _fixture.CreateGraphQlDbContext();
        var contractSnapshot = await assertContext.ContractSnapshots.SingleAsync();
        contractSnapshot.ContractAddressIndex.Should().Be(contractAddress.Index);
        contractSnapshot.Amount.Should().Be(expectedAmount);
        contractSnapshot.ModuleReference.Should().Be(expectedModuleReference);
        contractSnapshot.ContractName.Should().Be(expectedContractName);
    }
    
    [Fact]
    public async Task GivenMultipleEvents_WhenHavingInitializationEvent_ThenSaveCorrectAmount()
    {
        // Arrange
        await DatabaseFixture.TruncateTables("graphql_contract_snapshot", "graphql_module_reference_contract_link_events", "graphql_contract_events", "graphql_contracts");
        var dbContextFactory = _fixture.CreateDbContractFactoryMock().Object;
        var contractRepository = await ContractRepository.Create(dbContextFactory);
        var repositoryFactory = new RepositoryFactory(dbContextFactory);
        var contractAddress = new ContractAddress(0, 0);
        var someAccount = new AccountAddress("");
        var other = new ContractAddress(2, 1);
        const string expectedModuleReference = "foobar";
        const string expectedContractName = "foo";
        const ulong expectedAmount = 42;
        var link = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithModuleReference(expectedModuleReference)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var contractEvents = new List<ContractEvent>{
            // Add Contract Initialized
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEvent(new ContractInitialized("", new ContractAddress(1,0), 10, $"init_{expectedContractName}", ContractVersion.V0, Array.Empty<string>())).Build(),
            // Subtract Transferred
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(2)
                .WithEvent(new Transferred(2, contractAddress, someAccount)).Build(),
            // Add Contract Updated
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(3)
                .WithEvent(new ContractUpdated(new ContractAddress(1,0), other, 42, "", "", ContractVersion.V0,  Array.Empty<string>())).Build(),
            // Subtract Contract Call
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(4)
                .WithEvent(new ContractCall(new ContractUpdated(other, contractAddress, 8, "", "", ContractVersion.V0,  Array.Empty<string>()))).Build(), 
        }; // Total amount 42
        await contractRepository.AddAsync(link);
        await contractRepository.AddRangeAsync(contractEvents);
        
        // Act
        await ContractSnapshot.ImportContractSnapshot(repositoryFactory, contractRepository, ImportSource.DatabaseImport);
        await contractRepository.CommitAsync();

        // Assert
        await using var assertContext = _fixture.CreateGraphQlDbContext();
        var contractSnapshot = await assertContext.ContractSnapshots.SingleAsync();
        contractSnapshot.ContractAddressIndex.Should().Be(contractAddress.Index);
        contractSnapshot.Amount.Should().Be(expectedAmount);
        contractSnapshot.ModuleReference.Should().Be(expectedModuleReference);
        contractSnapshot.ContractName.Should().Be(expectedContractName);
    }
    
    [Fact]
    public void WhenGetAmount_ThenReturnCorrectAmount()
    {
        // Arrange
        var contractAddress = new ContractAddress(1, 1);
        var other = new ContractAddress(2, 1);
        var someAccount = new AccountAddress("");
        var contractEvents = new List<ContractEvent>{
            // Add Contract Initialized
            ContractEventBuilder.Create().WithEvent(new ContractInitialized("", new ContractAddress(1,0), 10, "", ContractVersion.V0, Array.Empty<string>())).Build(),
            // Subtract Transferred
            ContractEventBuilder.Create().WithEvent(new Transferred(2, contractAddress, someAccount)).Build(),
            // Add Contract Updated
            ContractEventBuilder.Create().WithEvent(new ContractUpdated(new ContractAddress(1,0), other, 42, "", "", ContractVersion.V0,  Array.Empty<string>())).Build(),
            // Subtract Contract Call
            ContractEventBuilder.Create().WithEvent(new ContractCall(new ContractUpdated(other, contractAddress, 8, "", "", ContractVersion.V0,  Array.Empty<string>()))).Build(), 
        };
    
        // Act
        var amount = ContractSnapshot.GetAmount(contractEvents, contractAddress, 0);

        // Assert
        amount.Should().Be(42);
    }
}

