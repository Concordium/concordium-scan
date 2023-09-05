using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract;

public interface IContractRepositoryFactory
{
    Task<IContractRepository> CreateAsync();
}

internal sealed class ContractRepositoryFactory : IContractRepositoryFactory
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public ContractRepositoryFactory(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public async Task<IContractRepository> CreateAsync()
    {
        return new ContractRepository(await _dbContextFactory.CreateDbContextAsync());
    }
}