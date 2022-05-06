﻿using Application.Api.GraphQL.EfCore;
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
    
    [UseDbContext(typeof(GraphQlDbContext))]
    public Transaction GetTransaction([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .Single(tx => tx.Id == TransactionId);
    }
}