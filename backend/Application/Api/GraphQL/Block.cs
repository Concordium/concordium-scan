using Application.Api.GraphQL.EfCore;
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
    public SpecialEvents SpecialEvents { get; init; }
    public FinalizationSummary? FinalizationSummary { get; init; }

    [UsePaging]
    public IEnumerable<Transaction> GetTransactions([Service] GraphQlDbContext dbContext)
    {
        return dbContext.Transactions.Where(tx => tx.BlockId == Id);
    }
}

public class FinalizationSummary
{
    public string FinalizedBlockHash { get; init; }
    public long FinalizationIndex { get; init; }
    public long FinalizationDelay { get; init; }
    
    [UsePaging]
    public IEnumerable<FinalizationSummaryParty> Finalizers { get; init; }
}

public class FinalizationSummaryParty
{
    public long BakerId { get; init; } 
    public long Weight { get; init; }
    public bool Signed { get; init; }
}