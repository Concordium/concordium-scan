using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Accounts;

public class AccountStatementEntry
{
    [GraphQLIgnore]
    public long AccountId { get; set; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public AccountStatementEntryType EntryType { get; set; }
    
    public long Amount { get; set; }

    public ulong AccountBalance { get; set; }

    /// <summary>
    /// Reference to the block containing the reward or the transaction that resulted in this entry. 
    /// Not directly part of graphql schema but exposed indirectly through the reference field.
    /// </summary>
    [GraphQLIgnore]
    public long BlockId { get; set; }

    /// <summary>
    /// Reference to the transaction that resulted in this entry. Will be null for rewards.
    /// Not directly part of graphql schema but exposed indirectly through the reference field.
    /// </summary>
    [GraphQLIgnore]
    public long? TransactionId { get; set; }

    public async Task<IBlockOrTransactionUnion> GetReference(GraphQlDbContext dbContext)
    {
        if (TransactionId.HasValue)
            return await dbContext.Transactions.AsNoTracking()
                .SingleAsync(x => x.Id == TransactionId.Value);
        
        return await dbContext.Blocks.AsNoTracking()
            .SingleAsync(x => x.Id == BlockId);
    }
}
