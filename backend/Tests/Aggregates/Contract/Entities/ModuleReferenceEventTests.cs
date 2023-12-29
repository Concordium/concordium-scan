using System.Collections.Generic;
using System.IO;
using System.Threading;
using Application.Aggregates.Contract;
using Application.Aggregates.Contract.Entities;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;
using FluentAssertions;
using Moq;
using Tests.TestUtilities.Builders;
using ContractAddress = Application.Api.GraphQL.ContractAddress;

namespace Tests.Aggregates.Contract.Entities;

public sealed class ModuleReferenceEventTests
{
    [Theory]
    [InlineData("cis1-wccd-embedded-schema-v0-unversioned.wasm", ModuleSchemaVersion.V0, false, 0)]
    [InlineData("cis1-wccd-embedded-schema-v0-versioned.wasm.v0", ModuleSchemaVersion.Undefined, true, 0)]
    [InlineData("cis2-wccd-embedded-schema-v1-unversioned.wasm.v1", ModuleSchemaVersion.V1, true, 1)]
    [InlineData("cis2-wccd-embedded-schema-v1-versioned.wasm.v1", ModuleSchemaVersion.Undefined, true, 1)]
    public async Task WhenCreateModuleSchema_ThenParseWithCorrectVersion(string fileName, ModuleSchemaVersion version, bool trim, int moduleVersion)
    {
        var client = new Mock<IContractNodeClient>();
        var bytes = (await File.ReadAllBytesAsync($"./TestUtilities/TestData/{fileName}"));
        if (trim)
        {
            bytes = bytes[8..];
        }
        VersionedModuleSource module = moduleVersion == 0 ? new ModuleV0(bytes) : new ModuleV1(bytes);
        var queryResponseModuleSource = new QueryResponse<VersionedModuleSource>(BlockHash.From(new byte[32]), module);
        
        client.Setup(c => c.GetModuleSourceAsync(It.IsAny<IBlockHashInput>(), It.IsAny<ModuleReference>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(queryResponseModuleSource));
        
        // Act
        var moduleSchema = await ModuleReferenceEvent.ModuleSourceInfo.Create(client.Object, 0, Convert.ToHexString(new byte[32]));

        // Assert
        moduleSchema.ModuleSource.Should().Be(Convert.ToHexString(bytes));
        moduleSchema.ModuleSchemaVersion.Should().Be(version);
    }
    
    
    [Theory]
    [InlineData("module.schema_embedded.wasm.hex", "FFFF03010000000C00000054657374436F6E7472616374000000000001150200000003000000466F6F020300000042617202")]
    [InlineData("module.wasm.hex", null)]
    public async Task WhenCreateModuleSchema_ThenSchemaPresent(string fileName, string? schema)
    {
        // Arrange
        var client = new Mock<IContractNodeClient>();
        var load = (await File.ReadAllTextAsync($"./TestUtilities/TestData/{fileName}")).Trim();
        var queryResponseModuleSource = new QueryResponse<VersionedModuleSource>(BlockHash.From(new byte[32]), new ModuleV1(Convert.FromHexString(load)));
        
        client.Setup(c => c.GetModuleSourceAsync(It.IsAny<IBlockHashInput>(), It.IsAny<ModuleReference>(), It.IsAny<CancellationToken>()))
            .Returns(Task.FromResult(queryResponseModuleSource));
        
        // Act
        var moduleSchema = await ModuleReferenceEvent.ModuleSourceInfo.Create(client.Object, 0, Convert.ToHexString(new byte[32]));

        // Assert
        moduleSchema.ModuleSource.Should().Be(load);
        if (schema is not null)
        {
            moduleSchema.Schema.Should().NotBeNull();
            moduleSchema.Schema.Should().Be(schema);
        }
        else
        {
            moduleSchema.Schema.Should().BeNull();
        }
    }
    
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
