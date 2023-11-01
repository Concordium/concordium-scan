using System.IO;
using Application.Aggregates.Contract.Exceptions;
using Application.Types;
using FluentAssertions;
using Moq;
using Serilog;

namespace Tests.Types;

public sealed class ReceiveNameTests
{
    [Fact]
    public void WhenGivenWrongReceiveName_ThenThrowException()
    {
        // Arrange
        const string receiveName = "foo";
        
        // Act
        var action = () => { _ = new ReceiveName(receiveName); };
        
        // Assert
        action.Should().Throw<ParsingException>();
    }

    [Fact]
    public async Task WhenDeserializeMessage_ThenReturnedParsed()
    {
        // Arrange
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string message = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        const string expectedMessage = "{\"data\":\"\",\"to\":{\"Account\":[\"3fpkgmKcGDKGgsDhUQEBAQXbFZJQw97JmbuhzmvujYuG1sQxtV\"]}}";
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        var receiveName = new ReceiveName($"{contractName}.{entrypoint}");
        
        // Act
        var deserializeMessage = receiveName.DeserializeMessage(message, schema, null, Mock.Of<ILogger>(), "");

        // Assert
        deserializeMessage.Should().Be(expectedMessage);
    }
}
