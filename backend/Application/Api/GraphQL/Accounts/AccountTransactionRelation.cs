using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Accounts;

public class AccountTransactionRelation
{
    /// <summary>
    /// Not part of schema. Only here to be able to query relations in correct order and for generating cursor values. 
    /// </summary>
    [GraphQLIgnore]
    public long Index { get; set; }

    /// <summary>
    /// Not part of schema. Only here to be able to query relations for specific accounts. 
    /// </summary>
    [GraphQLIgnore]
    public long AccountId { get; set; }

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
}
