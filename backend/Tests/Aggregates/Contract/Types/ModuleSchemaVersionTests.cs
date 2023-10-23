using Application.Aggregates.Contract.Types;
using FluentAssertions;

namespace Tests.Aggregates.Contract.Types;

public sealed class ModuleSchemaVersionTests
{
    [Theory]
    [InlineData(ModuleSchemaVersion.V0, (byte)0)]
    [InlineData(ModuleSchemaVersion.V2, (byte)2)]
    public void GivenVersion_WhenMapModuleSchemaVersionToFFIOption_ThenOptionContainsVersion(ModuleSchemaVersion version, byte mapped)
    {
        // Act
        var ffiOption = ModuleSchemaVersionExtensions.Into(version);

        // Assert
        ffiOption.is_some.Should().Be(1);
        ffiOption.t.Should().Be(mapped);
    }
    
    [Fact]
    public void GivenUndefinedVersion_WhenMapModuleSchemaVersionToFFIOption_ThenOptionEmpty()
    {
        // Act
        var ffiOption = ModuleSchemaVersionExtensions.Into(ModuleSchemaVersion.Undefined);

        // Assert
        ffiOption.is_some.Should().Be(0);
    }
    
}
