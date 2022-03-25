﻿using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class AccountReleaseScheduleItem
{
    /// <summary>
    /// Not part of schema. Only here to be able to query relations for specific accounts. 
    /// </summary>
    [GraphQLIgnore]
    public long AccountId { get; set; }

    /// <summary>
    /// Not part of schema. Only here to be able to query transaction. 
    /// </summary>
    [GraphQLIgnore]
    public long TransactionId { get; set; }

    /// <summary>
    /// Not part of schema. Index is mostly there to ensure primary key is unique in very theoretical situations. 
    /// </summary>
    [GraphQLIgnore]
    public long Index { get; set; }
    
    public DateTimeOffset Timestamp { get; set; }
    
    public ulong Amount { get; set; }
    
    /// <summary>
    /// Not part of schema. Field used for some internal querying of amounts locked in release schedules. 
    /// </summary>
    [GraphQLIgnore]
    public long FromAccountId { get; set; }

    [UseDbContext(typeof(GraphQlDbContext))]
    public async Task<Transaction> GetTransaction([ScopedService] GraphQlDbContext dbContext)
    {
        return await dbContext.Transactions.AsNoTracking()
            .SingleAsync(x => x.Id == TransactionId);
    }
}