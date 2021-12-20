using HotChocolate;
using HotChocolate.Types;
using HotChocolate.Types.Relay;

namespace Application.Api.GraphQL;

public class Block
{
    [ID]
    public long Id { get; set; }
    public string BlockHash { get; set; }
    public int BlockHeight { get; set; }
    public DateTimeOffset BlockSlotTime { get; init; }
    public int? BakerId { get; init; }
    public bool Finalized { get; init; }
    public int TransactionCount { get; init; }

    [UsePaging]
    public IEnumerable<Transaction> GetTransactions([Service] SampleDataSet sampleDataSet)
    {
        return sampleDataSet.AllTransactions.Where(tx => tx.BlockHeight == BlockHeight && tx.BlockHash == BlockHash);
    }
}