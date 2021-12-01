namespace ConcordiumSdk.Transactions;

public interface IAccountTransactionPayload
{
    byte[] SerializeToBytes();
    int GetBaseEnergyCost();
}