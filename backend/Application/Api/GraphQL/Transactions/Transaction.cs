using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

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
    
    internal static Transaction MapTransaction(BlockItemSummary value, long blockId)
    {
        return new Transaction
        {
            BlockId = blockId,
            TransactionIndex = (int)value.Index,
            TransactionHash = value.TransactionHash.ToString(),
            TransactionType = TransactionTypeUnion.CreateFrom(value.Details),
            SenderAccountAddress =  value.TryGetSenderAccount(out var sender) ?
                AccountAddress.From(sender!) : null,
            CcdCost = value.GetCost().Value,
            EnergyCost = value.EnergyCost.Value,
            RejectReason = value.TryGetRejectedAccountTransaction(out var rejectReason) ?
                TransactionRejectReason.MapRejectReason(rejectReason!) : null,
        };
    }
}
