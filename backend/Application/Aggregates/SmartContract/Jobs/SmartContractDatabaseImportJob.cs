using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.SmartContract.Configurations;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.SmartContract.Jobs;

internal class SmartContractDatabaseImportJob
{
    private const string JobName = "SmartContractDatabaseImportJob";
    
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ILogger _logger;
    private readonly SmartContractAggregateJobOptions _jobOptions;

    public SmartContractDatabaseImportJob(
        ISmartContractRepositoryFactory repositoryFactory,
        IOptions<SmartContractAggregateOptions> options
        )
    {
        _repositoryFactory = repositoryFactory;
        _logger = Log.ForContext<SmartContractDatabaseImportJob>();
        var gotJobOptions = options.Value.Jobs.TryGetValue(JobName, out var jobOptions);
        _jobOptions =  new SmartContractAggregateJobOptions();
    }

    private long _readCount;
    
    public async Task StartImport(CancellationToken token)
    {
        try
        {
            var smartContractAggregate = new SmartContractAggregate(_repositoryFactory);
            _readCount = 0;
            
            while (!token.IsCancellationRequested)
            {
                var finalHeight = await GetFinalHeight(token);
                
                if (finalHeight - _readCount <= _jobOptions.Limit)
                {
                    break;
                }

                var tasks = new Task[_jobOptions.NumberOfTask];
                for (var i = 0; i < _jobOptions.NumberOfTask; i++)
                {
                    tasks[i] = Task.Run(() => Run(smartContractAggregate, finalHeight, token), token);
                }

                await Task.WhenAll(tasks);

                _readCount = finalHeight;
            }
            
            _logger.Information($"Done with job {nameof(SmartContractDatabaseImportJob)}");
        }
        catch (Exception e)
        {
            // TODO: Set health state to unhealthy
            _logger.Fatal(e, $"{nameof(SmartContractDatabaseImportJob)} stopped due to exception.");
        }
    }

    private async Task<long> GetFinalHeight(CancellationToken token)
    {
        await using var context = await _repositoryFactory.CreateAsync();

        var finalHeight = await context.GetLatestImportState(token);

        return finalHeight;
    }

    private async Task Run(SmartContractAggregate contractAggregate, long finalHeight, CancellationToken token)
    {
        while (_readCount < finalHeight && !token.IsCancellationRequested)
        {
            var height = Interlocked.Increment(ref _readCount);
            if (height > finalHeight)
            {
                return;
            }

            await contractAggregate.DatabaseImportJob(height, token);
        }
    }
}