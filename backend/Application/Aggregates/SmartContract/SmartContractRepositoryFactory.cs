using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.SmartContract;

internal sealed class SmartContractRepositoryFactory : ISmartContractRepositoryFactory
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public SmartContractRepositoryFactory(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public async Task<ISmartContractRepository> CreateAsync()
    {
        return new SmartContractRepository(await _dbContextFactory.CreateDbContextAsync());
    }
}