using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Common.Diagnostics;
using Application.Import.ConcordiumNode;
using Application.Resilience;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Changed calculations of finalization time to use last finalized block hash from block info instead of
/// using finalization summaries.
/// 
/// Changes are needed since finalization summaries isn't present in protocol 6.
/// 
/// Using latest finalization block hash in block info is a robust calculation in both old (before protocol 6)
/// and new consensus protocol.
/// </summary>
public class _02_UpdateFinalizationTimes : IMainMigrationJob 
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IConcordiumNodeClient _client;
    private readonly ImportStateController _importStateController;
    private readonly BlockWriter _blockWriter;
    private readonly MetricsWriter _metricsWriter;
    private readonly ILogger _logger;
    private readonly MainMigrationJobOptions _options;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_02_UpdateFinalizationTimes";

    public _02_UpdateFinalizationTimes(
        IOptions<MainMigrationJobOptions> options,
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IMetrics metrics,
        IConcordiumNodeClient client,
        DatabaseSettings dbSettings
        )
    {
        _options = options.Value;
        _contextFactory = contextFactory;
        _client = client;
        _importStateController = new ImportStateController(contextFactory, metrics);
        _blockWriter = new BlockWriter(contextFactory, metrics);
        _metricsWriter = new MetricsWriter(dbSettings, metrics);
        _logger = Log.ForContext<_02_UpdateFinalizationTimes>();
    }

    /// <summary>
    /// Updates the finalization time for a block if its finalization time has not been set within the 31 days leading
    /// up to the current <see cref="DateTimeOffset.UtcNow"/> until reaching the
    /// <see cref="ImportState.MaxImportedBlockHeight"/>.
    /// </summary>
    public async Task StartImport(CancellationToken token)
    {
        _logger.Debug($"Start initial processing of {JobName}");
        var state = await _importStateController.GetStateIfExists();
        if (state == null)
        {
            return;
        }
        var fromDate = DateTimeOffset.UtcNow.Subtract(TimeSpan.FromDays(31));
        _logger.Information($"Migrating from date time: {fromDate}");
        
        var initialBlockHeight= state.MaxBlockHeightWithUpdatedFinalizationTime;
        var initialAbsoluteBlockHeight = new Absolute((ulong)initialBlockHeight);
        var initialBlockInfo = await _client.GetBlockInfoAsync(initialAbsoluteBlockHeight, token);
        if (initialBlockInfo.BlockSlotTime < fromDate)
        {
            _logger.Debug($"Updating max block height with updated finalization time since block slot time is {initialBlockInfo.BlockSlotTime}");
            var start = await BinarySearchBlockHeight(fromDate, state.MaxBlockHeightWithUpdatedFinalizationTime, state.MaxImportedBlockHeight, token);
            state.MaxBlockHeightWithUpdatedFinalizationTime = start;
            await _importStateController.SaveChanges(state);
        }
        _logger.Information($"Migrating from height: {state.MaxBlockHeightWithUpdatedFinalizationTime}");

        await IteratingFromLastSet(token);
    }

    private async Task<long> BinarySearchBlockHeight(DateTimeOffset finalBlockSlotTime, long start, long end, CancellationToken token = default)
    {
        while (start < end)
        {
            var mid = (start + end) / 2;
            var midAbsolute = new Absolute((ulong)mid);
            var midBlockInfo = await _client.GetBlockInfoAsync(midAbsolute, token);
            if (midBlockInfo.BlockSlotTime < finalBlockSlotTime)
            {
                start = mid + 1;
            }
            else
            {
                end = mid;
            }
        }
        return start;
    }
    
    /// <summary>
    /// Iterating from <see cref="ImportState.MaxBlockHeightWithUpdatedFinalizationTime"/>  up until today.
    ///
    /// Slow since each block needs to be iterated.
    /// </summary>
    private async Task IteratingFromLastSet(CancellationToken token)
    {
        _logger.Debug($"Start processing {JobName}");
        await Policies.GetTransientPolicy(GetUniqueIdentifier(), _logger, _options.RetryCount, _options.RetryDelay)
            .ExecuteAsync(async () =>
            {
                var state = await _importStateController.GetStateIfExists();
                if (state == null)
                {
                    return;
                }
                var startBlockHeight = state.MaxBlockHeightWithUpdatedFinalizationTime;
                var stopBlockHeight= state.MaxImportedBlockHeight;
    
                var nextBlockHeight = startBlockHeight;
                while (nextBlockHeight < stopBlockHeight)
                {
                    nextBlockHeight += 1;
                    _logger.Debug($"{JobName} processing block height {nextBlockHeight}");
                    var importState = await _importStateController.GetState();
                    var absolute = new Absolute((ulong)nextBlockHeight);
                    var blockInfo = await _client.GetBlockInfoAsync(absolute, token);
    
                    var finalizationTimeUpdate = await _blockWriter.UpdateFinalizationTimeOnBlocksInFinalizationProof(blockInfo, importState);
                    await _metricsWriter.UpdateFinalizationTimes(finalizationTimeUpdate);
                    await _importStateController.SaveChanges(importState);
                } 
            });
        _logger.Debug($"Done processing {JobName}");
    }

    public string GetUniqueIdentifier() => JobName;

    public bool ShouldNodeImportAwait() => true;
}
