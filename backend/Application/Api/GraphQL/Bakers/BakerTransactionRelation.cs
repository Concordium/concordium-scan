using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Extensions;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Bakers;

public class BakerTransactionRelation
{
    /// <summary>
    /// Not part of schema. Only here to be able to query relations for specific accounts. 
    /// </summary>
    [GraphQLIgnore]
    public long BakerId { get; set; }

    [ID]
    [GraphQLName("id")]
    public long Index { get; set; }
    
    /// <summary>
    /// Not part of schema. Only here to be able to retrieve the transaction. 
    /// </summary>
    [GraphQLIgnore]
    public long TransactionId { get; set; }
    
    public Transaction GetTransaction(GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .Single(tx => tx.Id == TransactionId);
    }
    
    internal static bool TryFrom(TransactionPair transactionPair, out BakerTransactionRelation? relation)
    {
        var bakerIds = transactionPair.Source.GetBakerIds().Distinct().ToArray();
        
        switch (bakerIds.Length)
        {
            case 0:
                relation = null;
                return false;
            case 1:
                relation = new BakerTransactionRelation
                {
                    BakerId = (long)bakerIds.Single().Id.Index,
                    TransactionId = transactionPair.Target.Id
                };
                return true;
            default:
                throw new InvalidOperationException("Did not expect multiple baker ids from one transaction");
        }
    }
}
