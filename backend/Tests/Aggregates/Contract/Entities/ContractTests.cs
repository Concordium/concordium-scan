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
    private readonly Application.Aggregates.Contract.Entities.Contract.ContractExtensions _contractExtensions = new();
    
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

