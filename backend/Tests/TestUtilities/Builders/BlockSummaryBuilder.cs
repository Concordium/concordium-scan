using ConcordiumSdk.NodeApi.Types;

namespace Tests.TestUtilities.Builders;

public class BlockSummaryBuilder
{
    private TransactionSummary[] _transactionSummaries;

    public BlockSummaryBuilder()
    {
        _transactionSummaries = new TransactionSummary[0];
    }

    public BlockSummary Build()
    {
        return new BlockSummary
        {
            SpecialEvents = new SpecialEvent[0],
            TransactionSummaries = _transactionSummaries.ToArray()
        };
    }

    public BlockSummaryBuilder WithTransactionSummaries(params TransactionSummary[] value)
    {
        _transactionSummaries = value;
        return this;
    }
}