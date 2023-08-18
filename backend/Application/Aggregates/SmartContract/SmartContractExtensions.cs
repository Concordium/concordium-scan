using Microsoft.Extensions.DependencyInjection;

namespace Application.Aggregates.SmartContract;

public static class SmartContractExtensions
{
    public static void AddSmartContractAggregate(this IServiceCollection collection)
    {
        collection.AddHostedService<SmartContractBackgroundService>();
        collection.AddTransient<ISmartContractRepositoryFactory, SmartContractRepositoryFactory>();
        collection.AddTransient<ISmartContractNodeClient, SmartContractNodeClient>();
    }
}