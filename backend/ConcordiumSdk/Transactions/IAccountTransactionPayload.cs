using ConcordiumSdk.Types;

namespace ConcordiumSdk.Transactions;

public interface IAccountTransactionPayload
{
    AccountTransactionType TransactionType { get; }
    byte[] SerializeToBytes();
    Amount GetBaseEnergyCost();
}