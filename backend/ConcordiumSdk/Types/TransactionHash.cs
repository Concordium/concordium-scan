namespace ConcordiumSdk.Types;

public class TransactionHash : Hash
{
    public TransactionHash(byte[] value) : base(value) {}
    public TransactionHash(string value) : base(value) {}
}