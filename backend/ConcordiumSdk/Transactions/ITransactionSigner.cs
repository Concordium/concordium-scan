namespace ConcordiumSdk.Transactions;

public interface ITransactionSigner
{
    byte[] Sign(byte[] bytes);
}