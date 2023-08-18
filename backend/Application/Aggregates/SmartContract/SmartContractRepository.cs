using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Aggregates.SmartContract;

public interface ISmartContractRepository : IAsyncDisposable
{
    IQueryable<T> GetReadOnlyQueryable<T>() where T : class;
    ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class;
    Task SaveChangesAsync(CancellationToken token);
}

public interface ISmartContractRepositoryFactory
{
    Task<ISmartContractRepository> CreateAsync();
}

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

internal sealed class SmartContractRepository : ISmartContractRepository
{
    private readonly GraphQlDbContext _context;
    
    public SmartContractRepository(GraphQlDbContext context)
    {
        _context = context;
    }
    
    public IQueryable<T> GetReadOnlyQueryable<T>() where T : class
    {
        return _context.Set<T>()
            .AsNoTracking()
            .AsQueryable();
    }

    public ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class
    {
        return _context.Set<T>().AddAsync(entity);
    }

    public Task SaveChangesAsync(CancellationToken token = default)
    {
        return _context.SaveChangesAsync(token);
    }

    public void Dispose()
    {
        _context.Dispose();
    }

    public ValueTask DisposeAsync()
    {
        return _context.DisposeAsync();
    }
}