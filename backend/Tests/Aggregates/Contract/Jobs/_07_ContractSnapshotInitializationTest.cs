using System.Collections.Generic;
using System.Threading;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Extensions;
using Application.Aggregates.Contract.Jobs;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Tests.TestUtilities;
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.Contract.Jobs;

[Collection(DatabaseCollectionFixture.DatabaseCollection)]
public class _07_ContractSnapshotInitializationTest
{
    private readonly DatabaseFixture _fixture;
    
    public _07_ContractSnapshotInitializationTest(DatabaseFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task WhenRunJob_ThenEnrichWithSnapshot()
    {
        // Arrange
        ContractExtensions.AddDapperTypeHandlers();
        await DatabaseFixture.TruncateTables("graphql_contract_snapshot", "graphql_module_reference_contract_link_events", "graphql_contract_events", "graphql_contracts");
        var contractAddress = new ContractAddress(0, 0);
        var other = new ContractAddress(2, 1);
        var someAccount = new AccountAddress("");
        const string expectedModuleReference = "foobar";
        const string expectedContractName = "foo";
        var moduleReferenceContractLinkEvent = ModuleReferenceContractLinkEventBuilder.Create()
            .WithContractAddress(contractAddress)
            .WithBlockHeight(6)
            .WithTransactionIndex(5)
            .WithEventIndex(4)
            .WithModuleReference(expectedModuleReference)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var contractEvents = new List<ContractEvent>{
            // Add Contract Initialized
            ContractEventBuilder.Create()
                .WithContractAddress(contractAddress)
                .WithEventIndex(1)
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
        var contract = ContractBuilder
            .Create()
            .WithContractAddress(contractAddress)
            .WithContractEvents(contractEvents)
            .Build();
        await using (var context = _fixture.CreateGraphQlDbContext())
        {
            await context.AddRangeAsync(contractEvents);
            await context.AddAsync(moduleReferenceContractLinkEvent);
            await context.AddAsync(contract);
            await context.SaveChangesAsync();
        }
        
        var options = Options.Create(new ContractAggregateOptions());
        var job = new _07_ContractSnapshotInitialization(
            _fixture.CreateDbContractFactoryMock().Object,
            options
        );
        var parallelBatchJob = new ParallelBatchJob<_07_ContractSnapshotInitialization>(job, options);
        
        // Act
        await parallelBatchJob.StartImport(CancellationToken.None);
        
        // Assert
        parallelBatchJob.ShouldNodeImportAwait().Should().BeTrue();
        await using var assertContext = _fixture.CreateGraphQlDbContext();
        var contractSnapshot = await assertContext.ContractSnapshots.SingleAsync();
        contractSnapshot.ContractAddressIndex.Should().Be(contractAddress.Index);
        contractSnapshot.Amount.Should().Be(42);
        contractSnapshot.ModuleReference.Should().Be(expectedModuleReference);
        contractSnapshot.ContractName.Should().Be(expectedContractName);
    }
}
