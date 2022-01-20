using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class Transaction
{
    [ID]
    public long Id { get; set; }
    
    /// <summary>
    /// Not part of schema. Clients must get block-specific data from the Blocks field.
    /// Only here to retrieve the the owning block. 
    /// </summary>
    [GraphQLIgnore]
    public long BlockId { get; set; }
    
    public int TransactionIndex { get; set; }
    
    public string TransactionHash { get; set; }
    
    public string? SenderAccountAddress { get; set; }
    
    public ulong CcdCost { get; set; }
    
    public ulong EnergyCost { get; set; }

    public TransactionTypeUnion TransactionType { get; set; }

    /// <summary>
    /// TODO: Document intend with property!
    /// </summary>
    [GraphQLIgnore]
    public string? RejectReason { get; set; }

    public TransactionResult Result
    {
        get
        {
            if (RejectReason == null) return new Successful(this);
            return new Rejected();
        }
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Block GetBlock([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .Single(block => block.Id == BlockId);
    }
}
