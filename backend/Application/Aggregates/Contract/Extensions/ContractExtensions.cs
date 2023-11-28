using Application.Aggregates.Contract.BackgroundServices;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Entities;
using Application.Aggregates.Contract.Jobs;
using Application.Aggregates.Contract.Observability;
using Application.Jobs;
using Dapper;
using HotChocolate.Execution.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Prometheus;

namespace Application.Aggregates.Contract.Extensions;

public static class ContractExtensions
{
    public static void AddContractAggregate(this IServiceCollection collection, IConfiguration configuration)
    {
        collection.Configure<ContractAggregateOptions>(configuration.GetSection("ContractAggregate"));

        collection.AddHostedService<ContractNodeImportBackgroundService>();
        
        collection.AddTransient<IContractRepositoryFactory, RepositoryFactory>();
        collection.AddTransient<IContractNodeClient, ContractNodeClient>();
        
        collection.AddContractJobs();
        collection.AddObservability();
        
        AddDapperTypeHandlers();
    }

    private static void AddObservability(this IServiceCollection collection)
    {
        collection.AddSingleton<ContractHealthCheck>();
        collection.AddHealthChecks()
            .AddCheck<ContractHealthCheck>("Contract", HealthStatus.Unhealthy)
            .ForwardToPrometheus();
    }
    
    /// <summary>
    /// Used by <see cref="Dapper"/> to specify custom mappings of types.
    /// </summary>
    internal static void AddDapperTypeHandlers()
    {
        SqlMapper.AddTypeHandler(new TransactionRejectReasonHandler());
        SqlMapper.AddTypeHandler(new TransactionResultEventHandler());
        SqlMapper.AddTypeHandler(new TransactionTypeUnionHandler());
        SqlMapper.AddTypeHandler(new AccountAddressHandler());
    }

    internal static IRequestExecutorBuilder AddContractGraphQlConfigurations(this IRequestExecutorBuilder builder)
    {
        builder
            .AddType<Entities.Contract.ContractQuery>()
            .AddTypeExtension<Entities.Contract.ContractExtensions>()
            .AddType<ModuleReferenceEvent.ModuleReferenceEventQuery>()
            .AddTypeExtension<ModuleReferenceEvent.ModuleReferenceEventExtensions>()
            .AddTypeExtension<ModuleReferenceContractLinkEvent.ModuleReferenceContractLinkEventExtensions>();
        return builder;
    }

    /// <summary>
    /// Background service which executes all jobs related to Smart Contracts.
    /// </summary>
    private static void AddContractJobs(this IServiceCollection collection)
    {
        collection.AddHostedService<JobsBackgroundService<IContractJob, ContractJob>>();
        collection.AddTransient<IJobFinder<IContractJob, ContractJob>, JobFinder<IContractJob, ContractJob>>();
        collection.AddSingleton<IJobRepository<ContractJob>, JobRepository<ContractJob>>();
        
        collection.AddTransient<IContractJob, ParallelBatchBlockHeightJob<InitialContractAggregateCatchUpJob>>();
        collection.AddTransient<InitialContractAggregateCatchUpJob>();
        collection.AddTransient<IContractJob, InitialModuleSourceCatchup>();
        collection.AddTransient<IContractJob, UpdateModuleSourceCatchup>();
        collection.AddTransient<IContractJob, ParallelBatchJob<InitialContractEventDeserializationFieldsCatchUpJob>>();
        collection.AddTransient<InitialContractEventDeserializationFieldsCatchUpJob>();
        collection.AddTransient<IContractJob, ParallelBatchJob<InitialContractRejectEventDeserializationFieldsCatchUpJob>>();
        collection.AddTransient<InitialContractRejectEventDeserializationFieldsCatchUpJob>();
    }
}
