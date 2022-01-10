namespace ConcordiumSdk.Types;

public class ModuleRef : Hash
{
    public ModuleRef(byte[] value) : base(value) {}
    public ModuleRef(string value) : base(value) {}
}