using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Exceptions;
using Application.Aggregates.Contract.Observability;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using Application.Observability;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

internal class ContractDatabaseImportJob : IContractJob
{
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "ContractDatabaseImportJob";
    
    private readonly ContractHealthCheck _healthCheck;
    private readonly IContractRepositoryFactory _repositoryFactory;
    private readonly ILogger _logger;
    private readonly ContractAggregateJobOptions _jobOptions;
    private readonly ContractAggregateOptions _contractAggregateOptions;

    public ContractDatabaseImportJob(
        IContractRepositoryFactory repositoryFactory,
        IOptions<ContractAggregateOptions> options,
        ContractHealthCheck healthCheck
        )
    {
        _repositoryFactory = repositoryFactory;
        _healthCheck = healthCheck;
        _logger = Log.ForContext<ContractDatabaseImportJob>();
        _contractAggregateOptions = options.Value;
        var gotJobOptions = _contractAggregateOptions.Jobs.TryGetValue(JobName, out var jobOptions);
        _jobOptions = gotJobOptions ? jobOptions! : new ContractAggregateJobOptions();
    }

    private long _readCount;

    public async Task StartImport(CancellationToken token)
    {
        using var _ = TraceContext.StartActivity(nameof(ContractDatabaseImportJob));
        
        try
        {
            _readCount = -1;
            
            while (!token.IsCancellationRequested)
            {
                var finalHeight = await GetFinalHeight(token);
                
                if (finalHeight < (_readCount + 1) * _jobOptions.BatchSize)
                {
                    break;
                }

                var tasks = new Task[_jobOptions.MaxParallelTasks];
                for (var i = 0; i < _jobOptions.MaxParallelTasks; i++)
                {
                    tasks[i] = RunBatch(finalHeight, token);
                }

                using var cts = CancellationTokenSource.CreateLinkedTokenSource(token);
                var metricUpdater = UpdateReadHeightMetric(cts.Token);

                await Task.WhenAll(tasks);
                cts.Cancel();
                await metricUpdater;
                
                // Each task has done one increment which they didn't process.
                _readCount -= _jobOptions.MaxParallelTasks;
            }
            
            _logger.Information($"Done with job {nameof(ContractDatabaseImportJob)}");
        }
        catch (Exception e)
        {
            _logger.Fatal(e, $"{nameof(ContractDatabaseImportJob)} stopped due to exception.");
            _healthCheck.AddUnhealthyJobWithMessage(GetUniqueIdentifier(), "Database import job stopped due to exception.");
            _logger.Fatal(e, $"{nameof(ContractDatabaseImportJob)} stopped due to exception.");
            throw;
        }
    }

    /// <inheritdoc/>
    public string GetUniqueIdentifier()
    {
        return JobName;
    }

    private async Task UpdateReadHeightMetric(CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                var repository = await _repositoryFactory.CreateAsync();
                var latest = await repository.GetReadOnlyLatestContractReadHeight();
                if (latest != null)
                {
                    ContractMetrics.SetReadHeight(latest.BlockHeight, ImportSource.DatabaseImport);
                }

                await Task.Delay(_contractAggregateOptions.MetricDelay, token);
            }
        }
        catch (TaskCanceledException)
        {
            // Thrown from `Task.Delay` when token is cancelled. We don't want to have this rethrown but just
            // stop loop.
        }
    }

    private async Task<long> GetFinalHeight(CancellationToken token)
    {
        await using var context = await _repositoryFactory.CreateAsync();

        var finalHeight = await context.GetReadOnlyLatestImportState(token);

        return finalHeight;
    }
    /// <summary>
    /// Run each batch up to final height.
    ///
    /// Atomically get next batch interval from `_readCount`. If intervals get above <see cref="finalHeight"/> then
    /// processing stops.
    /// </summary>
    private async Task RunBatch(long finalHeight, CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            using var _ = TraceContext.StartActivity(nameof(RunBatch));
            
            var height = Interlocked.Increment(ref _readCount);
            var blockHeightTo = height * _jobOptions.BatchSize;
            if (blockHeightTo > finalHeight)
            {
                return;
            }
            var blockHeightFrom = Math.Max((height - 1) * _jobOptions.BatchSize + 1, 0);
            var affectedRows = await DatabaseBatchImportJob((ulong)blockHeightFrom, (ulong)blockHeightTo, token);

            if (affectedRows == 0) continue;
            _logger.Information("Written heights {From} to {To}", blockHeightFrom, blockHeightTo);   
        }
    }

    private async Task<ulong> DatabaseBatchImportJob(ulong heightFrom, ulong heightTo, CancellationToken token = default)
    {
        using var durationMetric = new ContractMetrics.DurationMetric(ImportSource.DatabaseImport);
        await using var repository = await _repositoryFactory.CreateAsync();
        var alreadyReadHeights = await repository.FromBlockHeightRangeGetBlockHeightsReadOrdered(heightFrom, heightTo);
        if (alreadyReadHeights.Count > 0)
        {
            _logger.Information("Following heights ranges has already been processed successfully and will be skipped {@Ranges}", PrettifySortedListToRanges(alreadyReadHeights));   
        }
        
        var affectedColumns = heightTo - heightFrom + 1 - (ulong)alreadyReadHeights.Count;
        if (affectedColumns == 0)
        {
            return affectedColumns;
        }

        var events = await repository.FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(heightFrom, heightTo);
        foreach (var eventDto in events.Where(e => !alreadyReadHeights.Contains((ulong)e.BlockHeight)))
        {
            if (!IsUsableTransaction(eventDto.TransactionType, eventDto.TransactionSender, eventDto.TransactionHash))
            {
                continue;
            }  
            
            await ContractAggregate.StoreEvent(
                ImportSource.DatabaseImport,
                repository,
                eventDto.Event,
                eventDto.TransactionSender!,
                (ulong)eventDto.BlockHeight,
                eventDto.BlockSlotTime.ToUniversalTime(),
                eventDto.TransactionHash,
                eventDto.TransactionIndex,
                eventDto.TransactionEventIndex
            );
        }

        await ContractAggregate.SaveLastReadBlocks(repository, heightFrom, heightTo, alreadyReadHeights, ImportSource.DatabaseImport);
        await repository.SaveChangesAsync(token);
        ContractMetrics.IncTransactionEvents(affectedColumns, ImportSource.DatabaseImport);
        return affectedColumns;
    }
    
    /// <summary>
    /// Validates if a transactions should be used and is valid.
    /// </summary>
    /// <exception cref="ContractImportException">
    /// If a event of type <see cref="AccountTransaction"/> is given, and hence the event should be evaluated,
    /// but transaction sender is zero.
    /// </exception>
    private static bool IsUsableTransaction(TransactionTypeUnion transactionType, AccountAddress? sender, string transactionHash)
    {
        if (transactionType is not AccountTransaction)
        {
            return false;
        }
        if (sender == null)
        {
            throw new ContractImportException(
                $"Not able to map transaction: {transactionHash}, since transaction sender was null");
        }

        return true;
    }

    /// <summary>
    /// Converts a sorted list of numbers into a list of tuple ranges, where each tuple indicates a
    /// sequential range in the input list.
    /// 
    /// For instance, for the input [1,2,3,5,6,8], the output will be [(1,3),(5,6),(8,8)].
    /// </summary>
    internal static IList<(ulong, ulong)> PrettifySortedListToRanges(IList<ulong> read)
    {
        var intervals = new List<(ulong,ulong)>();
        switch (read.Count)
        {
            // No interval exist.
            case 0:
                return intervals;
            // Only one singular "interval" exist.
            case 1:
                intervals.Add((read[0], read[0]));
                return intervals;
        }

        // If difference + 1 between first and last element are equal to lenght then all values are continuous. 
        if (read[^1] - read[0] + 1 == (ulong)read.Count)
        {
            intervals.Add((read[0], read[^1]));
            return intervals;
        }

        // Create intervals
        var firstElementOfRange = read[0];
        var previousRead = read[0];
        for (var i = 1; i < read.Count; i++)
        {
            var current = read[i];
            var previous = previousRead;
            
            previousRead = read[i];
            if (current == previous + 1)
            {
                // Values are continuous hence we are within interval.
                continue;
            }
            // Values not continuous since step between current and previous > 1. Hence add a interval.
            intervals.Add((firstElementOfRange, previous));
            // Set start of next interval to current read value.
            firstElementOfRange = current;
            
        }
        // Add last interval
        intervals.Add((firstElementOfRange, read[^1]));

        return intervals;
    }
}
