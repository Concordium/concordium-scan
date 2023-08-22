using Application.Aggregates.SmartContract.BackgroundServices;
using Application.Aggregates.SmartContract.Jobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Aggregates.SmartContract.Extensions;

public static class SmartContractExtensions
{
    public static void AddSmartContractAggregate(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.Configure<SmartContractAggregate>(configuration.GetSection("SmartContractAggregate"));

        collection.AddHostedService<SmartContractNodeImportBackgroundService>();
        
        collection.AddTransient<ISmartContractRepositoryFactory, SmartContractRepositoryFactory>();
        collection.AddTransient<ISmartContractNodeClient, SmartContractNodeClient>();
        
        collection.AddSmartContractJobs();
    }

    /// <summary>
    /// Background service which executes all jobs related to Smart Contracts.
    ///
    /// When new is implemented they should be added to the <see cref="SmartContractJobsBackgroundService"/>.
    /// </summary>
    private static void AddSmartContractJobs(this IServiceCollection collection)
    {
        collection.AddHostedService<SmartContractJobsBackgroundService>();
        collection.AddTransient<ISmartContractJobFinder, SmartContractJobFinder>();
        
        collection.AddTransient<ISmartContractJob, SmartContractDatabaseImportJob>();
    }
}