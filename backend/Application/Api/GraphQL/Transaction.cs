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
    
    [GraphQLDeprecated("Use the `block` field instead")]
    [ID(nameof(Block))]
    public long BlockId { get; set; }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [GraphQLDeprecated("Use the `block` field instead")]
    public string GetBlockHash([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .Single(x => x.Id == BlockId).BlockHash;
    }

    [UseDbContext(typeof(GraphQlDbContext))]
    [GraphQLDeprecated("Use the `block` field instead")]
    public long GetBlockHeight([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .Single(x => x.Id == BlockId).BlockHeight;
    }
    
    public int TransactionIndex { get; set; }
    
    public string TransactionHash { get; set; }
    
    public string? SenderAccountAddress { get; set; }
    
    public ulong CcdCost { get; set; }
    
    public ulong EnergyCost { get; set; }

    public TransactionTypeUnion TransactionType { get; set; }

    public TransactionResult Result
    {
        get
        {
            if (RejectReason == null) return new Successful(this);
            return new Rejected();
        }
    }

    /// <summary>
    /// TODO: Document intend with property!
    /// </summary>
    [GraphQLIgnore]
    public string? RejectReason { get; set; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public Block GetBlock([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Blocks
            .AsNoTracking()
            .Single(block => block.Id == BlockId);
    }
}
