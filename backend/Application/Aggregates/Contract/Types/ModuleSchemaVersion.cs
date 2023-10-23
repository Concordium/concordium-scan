using Application.Interop;

namespace Application.Aggregates.Contract.Types;

public enum ModuleSchemaVersion
{
    Undefined = -1,
    V0 = 0,
    V1 = 1,
    V2 = 2,
    V3 = 3
}

internal static class ModuleSchemaVersionExtensions
{
    internal static InteropBinding.FFIByteOption Into(ModuleSchemaVersion? version)
    {
        return version is null or ModuleSchemaVersion.Undefined
            ? InteropBinding.FFIByteOption.None()
            : InteropBinding.FFIByteOption.Some((byte)version);
    }
}
