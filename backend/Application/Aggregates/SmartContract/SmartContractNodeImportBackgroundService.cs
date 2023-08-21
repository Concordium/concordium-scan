using System.Threading;
using System.Threading.Tasks;
using Application.Common.FeatureFlags;
using Microsoft.Extensions.Hosting;

namespace Application.Aggregates.SmartContract;

public class SmartContractNodeImportBackgroundService : BackgroundService
{
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ISmartContractNodeClient _client;
    private readonly IFeatureFlags _featureFlags;
    private readonly ILogger _logger;

    public SmartContractNodeImportBackgroundService(
        ISmartContractRepositoryFactory repositoryFactory,
        ISmartContractNodeClient client,
        IFeatureFlags featureFlags
        )
    {
        _repositoryFactory = repositoryFactory;
        _client = client;
        _featureFlags = featureFlags;
        _logger = Log.ForContext<SmartContractNodeImportBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }
        
        var smartContractAggregate = new SmartContractAggregate(_repositoryFactory);

        try
        {
            await smartContractAggregate.NodeImportJob(_client, stoppingToken);
        }
        catch (Exception e)
        {
            _logger.Fatal(e, $"{nameof(SmartContractNodeImportBackgroundService)} stopped due to exception.");
            // TODO: Set health state to non healthy
        }
    }
}