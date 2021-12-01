using System.Collections.Generic;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.Transactions;

public class SimpleTransferPayload : IAccountTransactionPayload
{
    public SimpleTransferPayload(Amount amount, AccountAddress toAddress)
    {
        Amount = amount;
        ToAddress = toAddress;
    }

    public AccountTransactionType TransactionType => AccountTransactionType.SimpleTransfer;
    public Amount Amount { get; }
    public AccountAddress ToAddress { get; }

    public byte[] SerializeToBytes()
    {
        var result = new List<byte>(40);
        result.AddRange(ToAddress.AsBytes);
        result.AddRange(Amount.SerializeToBytes());
        return result.ToArray();
    }

    public Amount GetBaseEnergyCost()
    {
        return Amount.FromMicroCcd(300);
    }
}