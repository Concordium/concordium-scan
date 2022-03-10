using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using HotChocolate.Types.Relay;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL;

public class Account
{
    [ID]
    public long Id { get; set; }

    [GraphQLIgnore] // Base address is only used internally for handling alias account addresses
    public AccountAddress BaseAddress { get; set; }
    
    [GraphQLName("address")]
    [GraphQLDeprecated("Use 'addressString' instead. Type of this field will be changed to AccountAddress in the near future.")]
    public string CanonicalAddress { get; set; }
    
    public string AddressString => CanonicalAddress;
    
    public DateTimeOffset CreatedAt { get; init; }
    
    [UseDbContext(typeof(GraphQlDbContext))]
    [UsePaging(InferConnectionNameFromField = false, ProviderName = "account_transaction_relation_by_descending_index")]
    public IQueryable<AccountTransactionRelation> GetTransactions([ScopedService] GraphQlDbContext dbContext)
    {
        return dbContext.AccountTransactionRelations
            .AsNoTracking()
            .Where(at => at.AccountId == Id)
            .OrderByDescending(x => x.Index);
    }
}