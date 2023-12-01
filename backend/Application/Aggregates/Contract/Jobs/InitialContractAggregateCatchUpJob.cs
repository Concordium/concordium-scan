using System.Threading;
using System.Threading.Tasks;
using Application.Aggregates.Contract.Configurations;
using Application.Aggregates.Contract.Exceptions;
using Application.Aggregates.Contract.Observability;
using Application.Aggregates.Contract.Types;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Transactions;
using Application.Resilience;
using Microsoft.Extensions.Options;

namespace Application.Aggregates.Contract.Jobs;

/// <summary>
/// Catch up contract aggregate state from existing data in database.
/// </summary>
public sealed class InitialContractAggregateCatchUpJob : IStatelessBlockHeightJobs
{
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "ContractDatabaseImportJob";
    
    private readonly IContractRepositoryFactory _repositoryFactory;
    private readonly IContractNodeClient _client;
    private readonly ILogger _logger;
    private readonly ContractAggregateOptions _contractAggregateOptions;

    public InitialContractAggregateCatchUpJob(
        IContractRepositoryFactory repositoryFactory,
        IContractNodeClient client,
        IOptions<ContractAggregateOptions> options
        )
    {
        _repositoryFactory = repositoryFactory;
        _client = client;
        _logger = Log.ForContext<InitialContractAggregateCatchUpJob>();
        _contractAggregateOptions = options.Value;
    }
    
    /// <inheritdoc/>
    public async Task<long> GetMaximumHeight(CancellationToken token)
    {
        await using var context = await _repositoryFactory.CreateContractRepositoryAsync();

        var finalHeight = await context.GetReadonlyLatestImportState(token);

        return finalHeight;
    }

    /// <inheritdoc/>
    public async Task UpdateMetric(CancellationToken token)
    {
        var repository = await _repositoryFactory.CreateContractRepositoryAsync();
        var latest = await repository.GetReadonlyLatestContractReadHeight();
        if (latest != null)
        {
            ContractMetrics.SetReadHeight(latest.BlockHeight, ImportSource.DatabaseImport);
        }
    }
    
    /// <inheritdoc/>
    public string GetUniqueIdentifier() => JobName;
    
    /// <inheritdoc/>
    public async Task<ulong> BatchImportJob(ulong heightFrom, ulong heightTo, CancellationToken token = default)
    {
        return await Policies.GetTransientPolicy<ulong>(GetUniqueIdentifier(), _logger, _contractAggregateOptions.RetryCount, _contractAggregateOptions.RetryDelay)
            .ExecuteAsync(async () =>
            {
                using var durationMetric =ContractMetrics.CreateContractReadDurationMetric(ImportSource.DatabaseImport);
                try
                {
                    await using var repository = await _repositoryFactory.CreateContractRepositoryAsync();
                    await using var moduleReadonlyRepository = await _repositoryFactory.CreateModuleReadonlyRepository();
                    var alreadyReadHeights = await repository.FromBlockHeightRangeGetBlockHeightsReadOrdered(heightFrom, heightTo);
                    if (alreadyReadHeights.Count > 0)
                    {
                        _logger.Information("Following heights ranges has already been processed successfully and will be skipped {@Ranges}", PrettifySortedListToRanges(alreadyReadHeights));   
                    }
        
                    var affectedRows = heightTo - heightFrom + 1 - (ulong)alreadyReadHeights.Count;
                    if (affectedRows == 0)
                    {
                        return affectedRows;
                    }

                    var totalEvents = 0u;
                    totalEvents += await StoreEvents(repository, moduleReadonlyRepository, alreadyReadHeights, heightFrom, heightTo);
                    totalEvents += await StoreRejected(repository, moduleReadonlyRepository, alreadyReadHeights, heightFrom, heightTo);

                    await ContractAggregate.SaveLastReadBlocks(repository, heightFrom, heightTo, alreadyReadHeights, ImportSource.DatabaseImport);
                    await repository.SaveChangesAsync(token);
                    ContractMetrics.IncTransactionEvents(totalEvents, ImportSource.DatabaseImport);
                    return affectedRows;
                }
                catch (Exception e)
                {
                    durationMetric.SetException(e);
                    throw;
                }
            });
    }

    public bool ShouldNodeImportAwait()
    {
        return true;
    }

    private static async Task<uint> StoreRejected(
        IContractRepository contractRepository,
        IModuleReadonlyRepository moduleReadonlyRepository,
        ICollection<ulong> alreadyReadHeights,
        ulong heightFrom,
        ulong heightTo)
    {
        var eventIndex = 0u;
        var rejections = await contractRepository.FromBlockHeightRangeGetContractRelatedRejections(heightFrom, heightTo);
        foreach (var rejectEventDto in rejections.Where(r => !alreadyReadHeights.Contains((ulong)r.BlockHeight)))
        {
            if (!IsUsableTransaction(rejectEventDto.TransactionType, rejectEventDto.TransactionSender, rejectEventDto.TransactionHash))
            {
                continue;
            }

            await ContractAggregate.StoreReject(
                ImportSource.DatabaseImport,
                contractRepository,
                moduleReadonlyRepository,
                rejectEventDto.RejectedEvent,
                rejectEventDto.TransactionSender!,
                (ulong)rejectEventDto.BlockHeight,
                rejectEventDto.BlockSlotTime.ToUniversalTime(),
                rejectEventDto.TransactionHash,
                rejectEventDto.TransactionIndex
            );
            eventIndex += 1;
        }

        return eventIndex;
    }
    
    
    private async Task<uint> StoreEvents(
        IContractRepository contractRepository, 
        IModuleReadonlyRepository moduleReadonlyRepository,
        ICollection<ulong> alreadyReadHeights, 
        ulong heightFrom, 
        ulong heightTo)
    {
        var addedEvents = 0u;
        var events = await contractRepository.FromBlockHeightRangeGetContractRelatedTransactionResultEventRelations(heightFrom, heightTo);
        var eventIndexMap = new Dictionary<(int BlockHeight, uint TransactionIndex), uint>();
        foreach (var eventDto in events
                     .Where(e => !alreadyReadHeights.Contains((ulong)e.BlockHeight))
                     .OrderBy(e => e.BlockHeight)
                     .ThenBy(e => e.TransactionIndex)
                     .ThenBy(e => e.TransactionEventIndex))
        {
            if (!IsUsableTransaction(eventDto.TransactionType, eventDto.TransactionSender, eventDto.TransactionHash))
            {
                continue;
            }

            if (!eventIndexMap.TryGetValue((eventDto.BlockHeight, eventDto.TransactionIndex), out var nextEventIndex))
            {
                nextEventIndex = 0;
            }
            
            var latestEventIndex = await ContractAggregate.StoreEvent(
                ImportSource.DatabaseImport,
                contractRepository,
                moduleReadonlyRepository,
                _client,
                eventDto.Event,
                eventDto.TransactionSender!,
                (ulong)eventDto.BlockHeight,
                eventDto.BlockSlotTime.ToUniversalTime(),
                eventDto.TransactionHash,
                eventDto.TransactionIndex,
                nextEventIndex
            );
            addedEvents += latestEventIndex - nextEventIndex + 1;
            eventIndexMap[(eventDto.BlockHeight, eventDto.TransactionIndex)] = latestEventIndex + 1;
        }

        return addedEvents;
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
