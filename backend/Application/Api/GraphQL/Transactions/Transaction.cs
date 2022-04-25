using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Transactions;

public class Transaction : IBlockOrTransactionUnion
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
    
    public AccountAddress? SenderAccountAddress { get; set; }

    [GraphQLDeprecated("Use 'senderAccountAddress.asString' instead. This field will be removed in the near future.")]
    public string? SenderAccountAddressString => SenderAccountAddress?.AsString;
    
    public ulong CcdCost { get; set; }
    
    public ulong EnergyCost { get; set; }

    public TransactionTypeUnion TransactionType { get; set; }

    /// <summary>
    /// Not part of GraphQL schema as the reject reason is part of the Result property
    /// which is created based on this property.
    /// Purpose of this property is to allow EF to persist/retrieve the reject reason. 
    /// </summary>
    [GraphQLIgnore]
    public TransactionRejectReason? RejectReason { get; set; }

    public TransactionResult Result
    {
        get
        {
            if (RejectReason == null) return new Success(this);
            return new Rejected(RejectReason);
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
