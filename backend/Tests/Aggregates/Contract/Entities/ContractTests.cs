using System.Collections.Generic;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.Contract.Entities;

public sealed class ContractTests
{
    private readonly Application.Aggregates.Contract.Entities.Contract.ContractExtensions _contractExtensions;

    public ContractTests()
    {
        _contractExtensions = new Application.Aggregates.Contract.Entities.Contract.ContractExtensions();
    }

    [Fact]
    public void WhenGetModuleReference_ThenReturnLatestAdded()
    {
        // Arrange
        const string expectedModuleReference = "foobar";
        var a = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(6)
            .WithTransactionIndex(5)
            .WithEventIndex(4)
            .WithModuleReference(expectedModuleReference)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var b = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(7)
            .WithTransactionIndex(5)
            .WithEventIndex(4)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed)
            .Build();
        var c = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(6)
            .WithTransactionIndex(4)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var d = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(5)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .Build();
        var contract = ContractBuilder
            .Create()
            .WithModuleReferenceContractLinkEvents(new List<ModuleReferenceContractLinkEvent>
            {
                a, b, c, d
            })
            .Build();

        // Act
        var moduleReference = _contractExtensions.GetModuleReference(contract);
        
        // Assert
        moduleReference.Should().Be(expectedModuleReference);
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
        var contract = ContractBuilder
            .Create()
            .WithContractAddress(contractAddress)
            .WithContractEvents(contractEvents)
            .Build();
        
        // Act
        var amount = _contractExtensions.GetAmount(contract);

        // Assert
        amount.Should().Be(42);
    }
}
