using Application.Exceptions;
using Concordium.Sdk.Types;

namespace Application.Import.ConcordiumNode.ConcordiumClientWrappers;

public interface IBlockItemSummaryWrapper
{
    ITransactionStatus GetTransactionStatus();
    BlockItemSummary GetFinalizedBlockItemSummary();
}

public class BlockItemSummaryWrapper : IBlockItemSummaryWrapper
{
    private readonly ITransactionStatus _transactionStatus;

    public BlockItemSummaryWrapper(ITransactionStatus transactionStatus)
    {
        _transactionStatus = transactionStatus;
    }
    public ITransactionStatus GetTransactionStatus() => _transactionStatus;

    /// <summary>
    /// Get block item summary for finalized transaction.
    /// </summary>
    /// <exception cref="ConcordiumClientWrapperException">Throws exception if the transaction
    /// it not finalized.
    /// </exception>
    public BlockItemSummary GetFinalizedBlockItemSummary()
    {
        if (_transactionStatus is not TransactionStatusFinalized finalized)
        {
            throw new ConcordiumClientWrapperException($"Transaction was of wrong type {_transactionStatus.GetType()}");
        }

        return finalized.State.Summary;
    }
}
