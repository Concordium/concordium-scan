using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Common.Diagnostics;
using Application.Import.ConcordiumNode;
using Application.Observability;
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
public class _03_UpdateFinalizationTimes : IMainMigrationJob 
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IConcordiumNodeClient _client;
    private readonly ImportStateController _importStateController;
    private readonly MetricsWriter _metricsWriter;
    private readonly ILogger _logger;
    private readonly MainMigrationJobOptions _options;

    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_03_UpdateFinalizationTimes";

    public _03_UpdateFinalizationTimes(
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
        _metricsWriter = new MetricsWriter(dbSettings, metrics);
        _logger = Log.ForContext<_03_UpdateFinalizationTimes>();
    }

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

                var initialValues = await FindInitialState(state, token);
                if (initialValues.IsEmpty())
                {
                    return;
                }
        
                var (nextHeight, priorFinalizedBlockHeight, priorFinalizedBlockHash, priorFinalizedBlotSlotTime, finalHeight) = initialValues;
                var initialHeight = nextHeight;
                while (nextHeight < finalHeight)
                {
                    nextHeight++;
                    _logger.Debug($"{JobName} processing block height {nextHeight}");
                    var nextBlockInfo = await _client.GetBlockInfoAsync(new Absolute((ulong)nextHeight), token);

                    var nextBlockLastFinalizedHash = nextBlockInfo.BlockLastFinalized.ToString();

                    // Last finalization block hasn't changed
                    if (nextBlockLastFinalizedHash == priorFinalizedBlockHash)
                    {
                        continue;
                    }

                    await using var context = await _contextFactory.CreateDbContextAsync(token);
                    var finalizedBlock = await context.Blocks
                        .SingleAsync(b => b.BlockHash == nextBlockLastFinalizedHash, token);

                    var timeUpdate = new FinalizationTimeUpdate(priorFinalizedBlockHeight, finalizedBlock.BlockHeight);
                    _logger.Debug($"Updating block height from:{timeUpdate.MinBlockHeight}, to {timeUpdate.MaxBlockHeight}");
                    
                    await BlockWriter.UpdateFinalizationTimes(timeUpdate, priorFinalizedBlotSlotTime, context);
                    await _metricsWriter.UpdateFinalizationTimes(timeUpdate);

                    priorFinalizedBlockHash = finalizedBlock.BlockHash;
                    priorFinalizedBlockHeight = finalizedBlock.BlockHeight;
                    priorFinalizedBlotSlotTime = finalizedBlock.BlockSlotTime;
                    ApplicationMetrics.SetJobCompletion(this, (nextHeight - initialHeight) / (double)finalHeight);
                }
            });
        _logger.Debug($"Done processing {JobName}");
    }
    
    private sealed record InitialState(
        int NextHeight,
        int PriorFinalizedBlockHeight,
        string PriorFinalizedBlockHash,
        DateTimeOffset PriorFinalizedBlotSlotTime,
        int FinalHeight
    )
    {
        internal static InitialState Empty() => new(0,0,string.Empty, DateTimeOffset.MinValue, 0);

        internal bool IsEmpty() => this.NextHeight == 0 && FinalHeight == 0;
    }

    private async Task<InitialState> FindInitialState(ImportState state, CancellationToken token)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(token);

        var firstBlockWithNull = await context
            .Blocks
            .Where(b => b.BlockStatistics.FinalizationTime == null)
            .OrderBy(b => b.BlockHeight)
            .Take(1)
            .SingleAsync(cancellationToken: token);

        if (firstBlockWithNull.BlockHeight > state.MaxBlockHeightWithUpdatedFinalizationTime)
        {
            return InitialState.Empty();
        }
            
        var priorBlockInfo = await _client.GetBlockInfoAsync(new Absolute((ulong)firstBlockWithNull.BlockHeight - 1), token);
        var priorBlockFinalization = await context.Blocks
            .SingleAsync(x => x.BlockHash == priorBlockInfo.BlockLastFinalized.ToString(), cancellationToken: token);
            
        var lastBlockWithNull = await context
            .Blocks
            .Where(b => b.BlockStatistics.FinalizationTime == null)
            .OrderByDescending(b => b.BlockHeight)
            .Take(1)
            .SingleAsync(cancellationToken: token);

        return new InitialState(
            firstBlockWithNull.BlockHeight,
            priorBlockFinalization.BlockHeight,
            priorBlockFinalization.BlockHash,
            priorBlockFinalization.BlockSlotTime,
            lastBlockWithNull.BlockHeight
        );
    }

    public string GetUniqueIdentifier() => JobName;

    public bool ShouldNodeImportAwait() => false;
}
