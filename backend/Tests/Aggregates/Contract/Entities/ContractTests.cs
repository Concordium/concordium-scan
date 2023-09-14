using System.Collections.Generic;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
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
        var from = new ContractAddress(1, 1);
        var to = new ContractAddress(2, 1);
        var contractEvents = new List<ContractEvent>{
            ContractEventBuilder.Create().WithEvent(new Transferred(42, from, to)).Build(),
            ContractEventBuilder.Create().WithEvent(new Transferred(2, to, from)).Build(),
            ContractEventBuilder.Create().WithEvent(new ContractInitialized("", new ContractAddress(1,0), 10, "", ContractVersion.V0, Array.Empty<string>())).Build(),
            ContractEventBuilder.Create().WithEvent(new ContractUpdated(new ContractAddress(1,0), new ContractAddress(1,0), 7, "", "", ContractVersion.V0,  Array.Empty<string>())).Build()
        };
        var contract = ContractBuilder
            .Create()
            .WithContractAddress(to)
            .WithContractEvents(contractEvents)
            .Build();
        
        // Act
        var amount = _contractExtensions.GetAmount(contract);

        // Assert
        amount.Should().Be(57);
    }
}