using Microsoft.Extensions.DependencyInjection;

namespace Application.Aggregates.SmartContract.Jobs;

public interface ISmartContractJobFinder
{
    IEnumerable<ISmartContractJob> GetJobs();
}

public sealed class SmartContractJobFinder : ISmartContractJobFinder
{
    private readonly IServiceProvider _provider;

    public SmartContractJobFinder(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public IEnumerable<ISmartContractJob> GetJobs()
    {
        return _provider.GetServices<ISmartContractJob>();
    }
}