using Application.Aggregates.Contract.Types;
using FluentAssertions;

namespace Tests.Aggregates.Contract.Types;

public sealed class ModuleSchemaVersionTests
{
    [Theory]
    [InlineData(ModuleSchemaVersion.Undefined, (byte)0)]
    [InlineData(ModuleSchemaVersion.V0, (byte)0)]
    [InlineData(ModuleSchemaVersion.V2, (byte)2)]
    public void WhenMapModuleSchemaVersionToFFIOption_ThenCorrect(
        ModuleSchemaVersion version, byte mapped
        )
    {
        // Act
        var ffiOption = ModuleSchemaVersionExtensions.Into(version);

        // Assert
        if (ModuleSchemaVersion.Undefined == version)
        {
            ffiOption.is_some.Should().Be(0);
        }
        else
        {
            ffiOption.is_some.Should().Be(1);
            ffiOption.t.Should().Be(mapped);
        }
    }
}
