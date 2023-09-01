using Microsoft.Extensions.DependencyInjection;

namespace Application.Aggregates.Contract.Jobs;

public sealed class ContractJobFinder : IContractJobFinder
{
    private readonly IServiceProvider _provider;

    public ContractJobFinder(IServiceProvider provider)
    {
        _provider = provider;
    }
    
    public IEnumerable<IContractJob> GetJobs()
    {
        return _provider.GetServices<IContractJob>();
    }
}