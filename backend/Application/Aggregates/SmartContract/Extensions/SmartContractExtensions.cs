using Application.Aggregates.SmartContract.BackgroundServices;
using Application.Aggregates.SmartContract.Configurations;
using Application.Aggregates.SmartContract.Jobs;
using Dapper;
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
        
        AddDapperTypeHandlers();
    }
    
    /// <summary>
    /// Used by <see cref="Dapper"/> to specify custom mappings of types.
    /// </summary>
    internal static void AddDapperTypeHandlers()
    {
        SqlMapper.AddTypeHandler(new TransactionResultEventHandler());
        SqlMapper.AddTypeHandler(new TransactionTypeUnionHandler());
        SqlMapper.AddTypeHandler(new AccountAddressHandler());
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