using System.Collections.Generic;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.Transactions;

public class SimpleTransferPayload : IAccountTransactionPayload
{
    public SimpleTransferPayload(CcdAmount amount, AccountAddress toAddress)
    {
        Amount = amount;
        ToAddress = toAddress;
    }

    public CcdAmount Amount { get; }
    public AccountAddress ToAddress { get; }

    public byte[] SerializeToBytes()
    {
        var result = new List<byte>(41);
        result.Add((byte)AccountTransactionType.SimpleTransfer);
        result.AddRange(ToAddress.AsBytes);
        result.AddRange(Amount.SerializeToBytes());
        return result.ToArray();
    }

    public int GetBaseEnergyCost()
    {
        return 300;
    }
}