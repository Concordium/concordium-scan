using ConcordiumSdk.Transactions;

namespace Tests.TestUtilities.Stubs;

public class TransactionSignerStub : ITransactionSigner
{
    public byte[] Sign(byte[] bytes)
    {
        return bytes;
    }
}