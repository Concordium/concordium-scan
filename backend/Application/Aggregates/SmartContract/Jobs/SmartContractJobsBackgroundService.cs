using System.Threading;
using System.Threading.Tasks;
using Application.Common.FeatureFlags;
using Microsoft.Extensions.Hosting;

namespace Application.Aggregates.SmartContract.Jobs;

/// <summary>
/// Common background service which executes background jobs related to Smart Contracts.
///
/// When new jobs are added they should be dependency injected and added to the constructor of this service.
/// </summary>
internal sealed class SmartContractJobsBackgroundService : BackgroundService
{
    private readonly SmartContractDatabaseImportJob _smartContractDatabaseImportJob;
    private readonly IFeatureFlags _featureFlags;
    private readonly ILogger _logger;

    public SmartContractJobsBackgroundService(
        SmartContractDatabaseImportJob smartContractDatabaseImportJob,
        IFeatureFlags featureFlags
        )
    {
        _smartContractDatabaseImportJob = smartContractDatabaseImportJob;
        _featureFlags = featureFlags;
        _logger = Log.ForContext<SmartContractJobsBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }

        await Task.WhenAll(_smartContractDatabaseImportJob.StartImport(stoppingToken));
        
        _logger.Information($"{nameof(SmartContractJobsBackgroundService)} done.");
    }
}