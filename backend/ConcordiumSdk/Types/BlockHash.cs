namespace ConcordiumSdk.Types;

public class BlockHash : Hash
{
    public BlockHash(byte[] value) : base(value) {}
    public BlockHash(string value) : base(value) {}
}