using Application.Api.GraphQL.EfCore;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Contracts
{
    [ExtendObjectType(typeof(Query))]
    public class ContractsQuery
    {
        [UseDbContext(typeof(GraphQlDbContext))]
        [UsePaging]
        public IQueryable<Contract> GetContracts([ScopedService] GraphQlDbContext dbContext)
        {
            return dbContext.SmartContractView.AsNoTracking();
        }
    }
}