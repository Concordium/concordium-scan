using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.SmartContract.Configurations;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.SmartContract.Jobs;

internal class SmartContractDatabaseImportJob : ISmartContractJob
{
    private const string JobName = "SmartContractDatabaseImportJob";
    
    private readonly ISmartContractRepositoryFactory _repositoryFactory;
    private readonly ILogger _logger;
    private readonly SmartContractAggregateJobOptions _jobOptions;
    private readonly SmartContractAggregateOptions _smartContractAggregateOptions;

    public SmartContractDatabaseImportJob(
        ISmartContractRepositoryFactory repositoryFactory,
        IOptions<SmartContractAggregateOptions> options
        )
    {
        _repositoryFactory = repositoryFactory;
        _logger = Log.ForContext<SmartContractDatabaseImportJob>();
        _smartContractAggregateOptions = options.Value;
        var gotJobOptions = _smartContractAggregateOptions.Jobs.TryGetValue(JobName, out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new SmartContractAggregateJobOptions();
    }

    private long _readCount;

    public async Task StartImport(CancellationToken token)
    {
        try
        {
            var smartContractAggregate = new SmartContractAggregate(_repositoryFactory, _smartContractAggregateOptions);
            _readCount = -1;
            
            while (!token.IsCancellationRequested)
            {
                var finalHeight = await GetFinalHeight(token);
                
                if (finalHeight < (_readCount + 1) * _jobOptions.BatchSize)
                {
                    break;
                }

                var tasks = new Task[_jobOptions.NumberOfTask];
                for (var i = 0; i < _jobOptions.NumberOfTask; i++)
                {
                    tasks[i] = Task.Run(() => RunBatch(smartContractAggregate, finalHeight, token), token);
                }

                await Task.WhenAll(tasks);

                // Each task has done one increment which they didn't process.
                _readCount -= _jobOptions.NumberOfTask;
            }
            
            _logger.Information($"Done with job {nameof(SmartContractDatabaseImportJob)}");
        }
        catch (Exception e)
        {
            _logger.Fatal(e, $"{nameof(SmartContractDatabaseImportJob)} stopped due to exception.");
            throw;
        }
    }

    public string GetUniqueIdentifier()
    {
        return JobName;
    }

    private async Task<long> GetFinalHeight(CancellationToken token)
    {
        await using var context = await _repositoryFactory.CreateAsync();

        var finalHeight = await context.GetLatestImportState(token);

        return finalHeight;
    }
    
    /// <summary>
    /// Run each batch up to final height.
    ///
    /// Atomically get next batch interval from `_readCount`. If intervals get above <see cref="finalHeight"/> then
    /// processing stops.
    /// </summary>
    private async Task RunBatch(SmartContractAggregate contractAggregate, long finalHeight, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            var height = Interlocked.Increment(ref _readCount);
            var blockHeightTo = height * _jobOptions.BatchSize;
            if (blockHeightTo > finalHeight)
            {
                return;
            }
            var blockHeightFrom = Math.Max((height - 1) * _jobOptions.BatchSize + 1, 0);

            var affectedRows = await contractAggregate.DatabaseBatchImportJob((ulong)blockHeightFrom, (ulong)blockHeightTo, token);

            if (affectedRows == 0) continue;
            _logger.Debug("Written heights {From} to {To}", blockHeightFrom, blockHeightTo);   
        }
    }
}