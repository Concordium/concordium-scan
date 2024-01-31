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
    /// Iterating from latest block height imported and then back to
    /// <see cref="ImportState.MaxBlockHeightWithUpdatedFinalizationTime"/>.
    ///
    /// Find <see cref="BlockInfo.BlockLastFinalized"/>, say Block To, and then find for Block To find
    /// <see cref="BlockInfo.BlockLastFinalized"/>, say Block From.
    ///
    /// Then for all blocks between Block From and Block To set finalization time as the time difference
    /// between the blocks slot time and the block slot time of Block To.
    /// </summary>
    public async Task StartImport(CancellationToken token)
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
                var stoppingBlockHeight = state.MaxBlockHeightWithUpdatedFinalizationTime;
                var initialBlockHeight= state.MaxImportedBlockHeight;
                var initialAbsoluteBlockHeight = new Absolute((ulong)initialBlockHeight);
                var initialBlockInfo = await _client.GetBlockInfoAsync(initialAbsoluteBlockHeight, token);
                var priorBlockSlotTime = initialBlockInfo.BlockSlotTime;
                await using var context = await _contextFactory.CreateDbContextAsync(token);
                
                var toBlock = await context
                    .Blocks
                    .SingleAsync(b => b.BlockHash == initialBlockInfo.BlockLastFinalized.ToString(), cancellationToken: token);
                var stateFinalUpdate = toBlock.BlockHeight;

                while (toBlock.BlockHeight > stoppingBlockHeight)
                {
                    var toAbsolute = new Absolute((ulong)toBlock.BlockHeight);
                    var toBlockInfo = await _client.GetBlockInfoAsync(toAbsolute, token);
                
                    var fromBlock = await context
                        .Blocks
                        .SingleAsync(b => b.BlockHash == toBlockInfo.BlockLastFinalized.ToString(), cancellationToken: token);

                    var timeUpdate = new FinalizationTimeUpdate(fromBlock.BlockHeight, toBlock.BlockHeight);
                    
                    _logger.Debug($"Updating block height from:{timeUpdate.MinBlockHeight}, to {timeUpdate.MaxBlockHeight}");

                    await BlockWriter.UpdateFinalizationTimes(timeUpdate, priorBlockSlotTime, context);
                    await _metricsWriter.UpdateFinalizationTimes(timeUpdate);

                    priorBlockSlotTime = toBlock.BlockSlotTime;
                    toBlock = fromBlock;                    
                }

                state.MaxBlockHeightWithUpdatedFinalizationTime = stateFinalUpdate;
                await _importStateController.SaveChanges(state);
            });
        _logger.Debug($"Done processing {JobName}");
    }
    
    /// <summary>
    /// Iterating from <see cref="ImportState.MaxBlockHeightWithUpdatedFinalizationTime"/>  up until today.
    ///
    /// Slow since each block needs to be iterated.
    /// </summary>
    public async Task IteratingFromLastSet(CancellationToken token)
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
