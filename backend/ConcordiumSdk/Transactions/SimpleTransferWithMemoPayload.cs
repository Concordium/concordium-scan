using System.Buffers.Binary;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.Transactions;

public class SimpleTransferWithMemoPayload : IAccountTransactionPayload
{
    public SimpleTransferWithMemoPayload(CcdAmount amount, AccountAddress toAddress, Memo memo)
    {
        Amount = amount;
        ToAddress = toAddress ?? throw new ArgumentNullException(nameof(toAddress));
        Memo = memo ?? throw new ArgumentNullException(nameof(memo));
    }

    public CcdAmount Amount { get; }
    public AccountAddress ToAddress { get; }
    public Memo Memo { get; }

    public byte[] SerializeToBytes()
    {
        var memoLength = Memo.AsBytes.Length;
        var serializedLength = 43 + memoLength;
        var result = new byte[serializedLength];
        
        Span<byte> buffer = result;
        buffer[0] = (byte)AccountTransactionType.SimpleTransferWithMemo;
        ToAddress.AsBytes.CopyTo(buffer.Slice(1, 32));
        BinaryPrimitives.WriteUInt16BigEndian(buffer.Slice(33, 2), Convert.ToUInt16(memoLength));
        Memo.AsBytes.CopyTo(buffer.Slice(35, memoLength));
        Amount.SerializeToBytes().CopyTo(buffer.Slice(35+memoLength, 8));
        
        return result;
    }

    public int GetBaseEnergyCost()
    {
        return 300;
    }
}