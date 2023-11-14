using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;

namespace Application.Aggregates.Contract;

public interface IContractRepositoryFactory
{
    Task<IContractRepository> CreateContractRepositoryAsync();

    Task<IModuleReadonlyRepository> CreateModuleReadonlyRepository();
}

internal sealed class RepositoryFactory : IContractRepositoryFactory
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public RepositoryFactory(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }
    
    public async Task<IContractRepository> CreateContractRepositoryAsync()
    {
        return new ContractRepository(await _dbContextFactory.CreateDbContextAsync());
    }

    public async Task<IModuleReadonlyRepository> CreateModuleReadonlyRepository()
    {
        return new ModuleReadonlyRepository(await _dbContextFactory.CreateDbContextAsync());
    }
}
