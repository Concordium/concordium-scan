using Application.Aggregates.SmartContract;

namespace Tests.Aggregates.SmartContract;

internal class TestSmartContractRepositoryFactory : ISmartContractRepositoryFactory
{
    private readonly TestSmartContractRepository _smartContractRepository;

    public TestSmartContractRepositoryFactory(TestSmartContractRepository smartContractRepository)
    {
        _smartContractRepository = smartContractRepository;
    }
    public Task<ISmartContractRepository> CreateAsync()
    {
        return Task.FromResult<ISmartContractRepository>(_smartContractRepository);
    }
}