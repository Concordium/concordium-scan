using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Transactions;

[ExtendObjectType(typeof(Query))]
public class TransactionsQuery
{
    private const int DefaultPageSize = 20;
    
    public Transaction? GetTransaction(GraphQlDbContext dbContext, [ID] long id)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefault(tx => tx.Id == id);
    }
    
    public Transaction? GetTransactionByTransactionHash(GraphQlDbContext dbContext, string transactionHash)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .SingleOrDefault(tx => tx.TransactionHash == transactionHash);
    }
    
    [UsePaging(MaxPageSize = 50, DefaultPageSize = DefaultPageSize, ProviderName = "transaction_by_descending_id")]
    public IQueryable<Transaction> GetTransactions(GraphQlDbContext dbContext)
    {
        return dbContext.Transactions
            .AsNoTracking()
            .OrderByDescending(tx => tx.Id);
    }
}
