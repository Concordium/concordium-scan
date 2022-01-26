using Application.Api.GraphQL;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders.GraphQL;

public class TransactionBuilder
{
    private long _id = 1;
    private string _transactionHash = "42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b";

    public Transaction Build()
    {
        return new Transaction
        {
            Id = _id,
            BlockId = 1,
            TransactionIndex = 0,
            TransactionHash = _transactionHash,
            SenderAccountAddress = "31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd",
            CcdCost = 241,
            EnergyCost = 422,
            TransactionType = new AccountTransaction { AccountTransactionType = AccountTransactionType.SimpleTransfer },
            RejectReason = null
        };
    }

    public TransactionBuilder WithId(long value)
    {
        _id = value;
        return this;
    }

    public TransactionBuilder WithTransactionHash(string value)
    {
        _transactionHash = value;
        return this;
    }
}