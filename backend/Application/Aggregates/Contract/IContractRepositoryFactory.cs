using System.Threading.Tasks;

namespace Application.Aggregates.Contract;

public interface IContractRepositoryFactory
{
    Task<IContractRepository> CreateAsync();
}