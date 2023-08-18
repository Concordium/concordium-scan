using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.Aggregates.SmartContract;

public interface ISmartContractRepository : IAsyncDisposable
{
    IQueryable<T> GetReadOnlyQueryable<T>() where T : class;
    ValueTask<EntityEntry<T>> AddAsync<T>(T entity) where T : class;
    Task SaveChangesAsync(CancellationToken token);
}