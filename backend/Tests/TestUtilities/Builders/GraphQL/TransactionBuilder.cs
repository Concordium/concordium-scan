using Application.Api.GraphQL.Transactions;
using Concordium.Sdk.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

namespace Tests.TestUtilities.Builders.GraphQL;

public class TransactionBuilder
{
    private long _id = 1;
    private string _transactionHash = "42b83d2be10b86bd6df5c102c4451439422471bc4443984912a832052ff7485b";
    private long _blockId = 1;
    private TransactionRejectReason? _rejectReason = null;
    private TransactionTypeUnion _transactionType = new AccountTransaction
        { AccountTransactionType = TransactionType.Transfer };

    public Transaction Build()
    {
        return new Transaction
        {
            Id = _id,
            BlockId = _blockId,
            TransactionIndex = 0,
            TransactionHash = _transactionHash,
            SenderAccountAddress = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
            CcdCost = 241,
            EnergyCost = 422,
            TransactionType = _transactionType,
            RejectReason = _rejectReason
        };
    }

    public TransactionBuilder WithTransactionType(TransactionTypeUnion transactionType)
    {
        _transactionType = transactionType;
        return this;
    }
    
    public TransactionBuilder WithRejectReason(TransactionRejectReason rejectReason)
    {
        _rejectReason = rejectReason;
        return this;
    }

    public TransactionBuilder WithBlockId(long blockId)
    {
        _blockId = blockId;
        return this;
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
