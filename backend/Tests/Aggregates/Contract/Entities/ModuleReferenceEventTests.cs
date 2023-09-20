using System.Collections.Generic;
using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL;
using FluentAssertions;
using Tests.TestUtilities.Builders;

namespace Tests.Aggregates.Contract.Entities;

public sealed class ModuleReferenceEventTests
{
    [Fact]
    public void WhenGetLinkedContract_ThenReturnThoseWhichHasNotBeenRemoved()
    {
        // Arrange
        var contractFirst = new ContractAddress(1, 0);
        var contractSecond = new ContractAddress(2, 0);
        var contractThird = new ContractAddress(3, 0);
        const string moduleReference = "fooBart";
        var firstEvent = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(1)
            .WithTransactionIndex(0)
            .WithEventIndex(0)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .WithModuleReference(moduleReference)
            .WithContractAddress(contractFirst)
            .Build();
        var secondEvent = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(1)
            .WithTransactionIndex(1)
            .WithEventIndex(0)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .WithModuleReference(moduleReference)
            .WithContractAddress(contractSecond)
            .Build();
        var thirdEvent = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(2)
            .WithTransactionIndex(0)
            .WithEventIndex(0)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed)
            .WithModuleReference(moduleReference)
            .WithContractAddress(contractFirst)
            .Build();
        var fourthEvent = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(2)
            .WithTransactionIndex(0)
            .WithEventIndex(1)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Added)
            .WithModuleReference(moduleReference)
            .WithContractAddress(contractThird)
            .Build();
        var fifthEvent = ModuleReferenceContractLinkEventBuilder.Create()
            .WithBlockHeight(2)
            .WithTransactionIndex(0)
            .WithEventIndex(2)
            .WithLinkAction(ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkAction.Removed)
            .WithModuleReference(moduleReference)
            .WithContractAddress(contractThird)
            .Build();
        var moduleReferenceContractLinkEvents = new List<ModuleReferenceContractLinkEvent>{firstEvent, secondEvent, thirdEvent, fourthEvent, fifthEvent};
        var moduleReferenceEvent = ModuleReferenceEventBuilder.Create()
            .WithModuleReferenceContractLinkEvent(moduleReferenceContractLinkEvents)
            .WithModuleReference(moduleReference)
            .Build();
        var moduleReferenceEventExtensions = new ModuleReferenceEvent.ModuleReferenceEventExtensions();
        
        // Act
        var linkedContracts = moduleReferenceEventExtensions.GetLinkedContracts(moduleReferenceEvent);
        
        // Assert
        linkedContracts.Count.Should().Be(1);
        linkedContracts[0].ContractAddress.Index.Should().Be(contractSecond.Index);
    }
}
