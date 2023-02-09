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

        [UseDbContext(typeof(GraphQlDbContext))]
        public Contract? GetContract([ScopedService] GraphQlDbContext dbContext, string address)
        {
            var contractAddress = new ContractAddress(address);

            return dbContext.SmartContractView
                .AsNoTracking()
                .SingleOrDefault(contract => contract.ContractAddress == contractAddress);
        }
    }
}
