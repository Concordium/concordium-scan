using System.IO;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using FluentAssertions;
using Moq;

namespace Tests.Api.GraphQL.Transactions;

public sealed class TransactionRejectReasonTests
{
    [Fact]
    public async Task GivenContractUpdated_WhenParseMessageAndEvents_ThenParsed()
    {
        // Arrange
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string message = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        const string expectedMessage = "{\"data\":\"\",\"to\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]}}";

        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        
        var moduleReferenceEvent = new ModuleReferenceEvent(
            0,
            "",
            0,
            0,
            "foo",
            new AccountAddress(""),
            "",
            schema,
            null,
            ImportSource.NodeImport,
            DateTimeOffset.UtcNow
        );
        
        var rejectedReceive = new RejectedReceive(
            1,
            new ContractAddress(1,0),
            $"{contractName}.{entrypoint}",
            message, null
        );

        var moduleRepositoryMock = new Mock<IModuleReadonlyRepository>();
        moduleRepositoryMock.Setup(m => m.GetModuleReferenceEventAtAsync(It.IsAny<ContractAddress>(), It.IsAny<ulong>(),
            It.IsAny<ulong>(), It.IsAny<uint>()))
            .Returns(Task.FromResult(moduleReferenceEvent));

        // Act
        var updated = await rejectedReceive.TryUpdateMessage(moduleRepositoryMock.Object, 0, 0);
        
        // Assert
        updated.Should().NotBeNull();
        updated!.Message.Should().Be(expectedMessage);
    }
    
}
