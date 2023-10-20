using System.IO;
using Application.Interop;
using FluentAssertions;
using VerifyXunit;

namespace Tests.Interop;

[UsesVerify]
public class InteropBindingTests
{
    [Fact]
    public async Task WhenSchemaDisplay_ThenReturnSchema()
    {
        // Arrange
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();

        // Act
        var (message, succeeded) = InteropBinding.SchemaDisplay(schema, InteropBinding.FFIOption.None());

        // Assert
        succeeded.Should().BeTrue();
        await Verifier.Verify(message)
            .UseFileName("module-schema")
            .UseDirectory("__snapshots__");
    }
    
    [Fact]
    public async Task WhenDisplayReceiveParam_ThenReturnParams()
    {
        // Arrange
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string value = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        
        // Act
        var (message, succeeded) = InteropBinding.GetReceiveContractParameter(schema, contractName, entrypoint, value,InteropBinding.FFIOption.None());

        // Assert
        succeeded.Should().BeTrue();
        await Verifier.Verify(message)
            .UseFileName("receive-params")
            .UseDirectory("__snapshots__");
    }
    
    [Fact]
    public async Task WhenDisplayEvent_ThenReturnEvent()
    {
        // Arrange
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        const string contractName = "cis2_wCCD";
        const string value = "fe00c0843d005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc79";
        
        // Act
        var (message, succeeded) = InteropBinding.GetEventContract(schema, contractName, value,InteropBinding.FFIOption.None());

        // Assert
        succeeded.Should().BeTrue();
        await Verifier.Verify(message)
            .UseFileName("event")
            .UseDirectory("__snapshots__");
    }
}
