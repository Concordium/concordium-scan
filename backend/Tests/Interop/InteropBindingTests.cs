using System.IO;
using Concordium.Sdk.Types;
using VerifyXunit;

namespace Tests.Interop;

[UsesVerify]
public class InteropBindingTests
{
    [Fact]
    public async Task GivenSchemaVersion_WhenSchemaDisplay_ThenReturnSchema()
    {
        // Arrange
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2-nft-schema")).Trim();
        var versionedModuleSchema = new VersionedModuleSchema(Convert.FromHexString(schema), ModuleSchemaVersion.V1);
        
        // Act
        var schemaDisplayed = versionedModuleSchema.GetDeserializedSchema().ToString();

        // Assert
        await Verifier.Verify(schemaDisplayed)
            .UseFileName("module-versioned-schema")
            .UseDirectory("__snapshots__");
    }
    
    [Fact]
    public async Task WhenSchemaDisplay_ThenReturnSchema()
    {
        // Arrange
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        var versionedModuleSchema = new VersionedModuleSchema(Convert.FromHexString(schema), ModuleSchemaVersion.Undefined);
        
        // Act
        var schemaDisplayed = versionedModuleSchema.GetDeserializedSchema().ToString();

        // Assert
        await Verifier.Verify(schemaDisplayed)
            .UseFileName("module-schema")
            .UseDirectory("__snapshots__");
    }
    
    [Fact]
    public async Task WhenDisplayReceiveParam_ThenReturnParams()
    {
        // Arrange
        var schema = (await File.ReadAllTextAsync("./TestUtilities/TestData/cis2_wCCD_sub")).Trim();
        var versionedModuleSchema = new VersionedModuleSchema(Convert.FromHexString(schema), ModuleSchemaVersion.Undefined);
        
        const string contractName = "cis2_wCCD";
        const string entrypoint = "wrap";
        const string value = "005f8b99a3ea8089002291fd646554848b00e7a0cd934e5bad6e6e93a4d4f4dc790000";
        
        // Act
        var message = Updated.GetDeserializeMessage(versionedModuleSchema, new ContractIdentifier(contractName),
            new EntryPoint(entrypoint), new Parameter(Convert.FromHexString(value))).ToString();

        // Assert
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
        var contractEvent = new ContractEvent(Convert.FromHexString(value));
        var versionedModuleSchema = new VersionedModuleSchema(Convert.FromHexString(schema), ModuleSchemaVersion.Undefined);
        
        // Act
        var deserializeEvent = contractEvent.GetDeserializeEvent(versionedModuleSchema, new ContractIdentifier(contractName)).ToString();

        // Assert
        await Verifier.Verify(deserializeEvent)
            .UseFileName("event")
            .UseDirectory("__snapshots__");
    }
}
