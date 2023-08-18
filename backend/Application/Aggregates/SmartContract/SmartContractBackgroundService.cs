using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Aggregates.SmartContract;

public class SmartContractBackgroundService : BackgroundService
{
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ISmartContractNodeClient _client;
    private readonly IFeatureFlags _featureFlags;
    private readonly ILogger _logger;

    public SmartContractBackgroundService(
        ISmartContractRepositoryFactory repositoryFactory,
        ISmartContractNodeClient client,
        IFeatureFlags featureFlags
        )
    {
        _repositoryFactory = repositoryFactory;
        _client = client;
        _featureFlags = featureFlags;
        _logger = Log.ForContext<SmartContractBackgroundService>();
    }
    
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_featureFlags.ConcordiumNodeImportEnabled)
        {
            _logger.Information("Import data from Concordium node is disabled. This controller will not run!");
            return;
        }
        
        var smartContractAggregate = new SmartContractAggregate(_repositoryFactory, _client);

        await smartContractAggregate.Import(stoppingToken);
    }
}