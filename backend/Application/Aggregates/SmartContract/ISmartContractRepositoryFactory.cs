using System.Threading.Tasks;

namespace Application.Aggregates.SmartContract;

public interface ISmartContractRepositoryFactory
{
    Task<ISmartContractRepository> CreateAsync();
}