using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Import;
using Application.Common.Diagnostics;
using Application.Import.ConcordiumNode;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Database.MigrationJobs;

/// <summary>
/// Set <see cref="ImportState.MaxBlockHeightWithUpdatedFinalizationTime"/> to
/// <see cref="BlockInfo.BlockLastFinalized"/> from <see cref="BlockInfo"/> for
/// block with height <see cref="ImportState.MaxImportedBlockHeight"/>.
///
/// Updating of finalization time prior to updated <see cref="ImportState.MaxBlockHeightWithUpdatedFinalizationTime"/>
/// is handled by <see cref="_03_UpdateFinalizationTimes"/>.
/// </summary>
public class _02_UpdateFinalizationTimeOnImportState : IMainMigrationJob
{
    private readonly IDbContextFactory<GraphQlDbContext> _contextFactory;
    private readonly IConcordiumNodeClient _client;
    private readonly ImportStateController _importStateController;
    private readonly ILogger _logger;
    
    /// <summary>
    /// WARNING - Do not change this if job already executed on environment, since it will trigger rerun of job.
    /// </summary>
    private const string JobName = "_02_UpdateFinalizationTimeOnImportState";

    public _02_UpdateFinalizationTimeOnImportState(
        IDbContextFactory<GraphQlDbContext> contextFactory,
        IMetrics metrics,
        IConcordiumNodeClient client
        )
    {
        _contextFactory = contextFactory;
        _client = client;
        _importStateController = new ImportStateController(contextFactory, metrics);
        _logger = Log.ForContext<_02_UpdateFinalizationTimeOnImportState>();
    }
    
    public async Task StartImport(CancellationToken token)
    {
        _logger.Debug($"Start processing {JobName}");
        
        var state = await _importStateController.GetStateIfExists();
        if (state == null)
        {
            return;
        }
        var maxImportedBlockHeight = new Absolute((ulong)state.MaxImportedBlockHeight);
        var maxImportedBlockInfo = await _client.GetBlockInfoAsync(maxImportedBlockHeight, token);
        
        await using var context = await _contextFactory.CreateDbContextAsync(token);
        var finalizedBlock = await context.Blocks
            .SingleAsync(x => x.BlockHash == maxImportedBlockInfo.BlockLastFinalized.ToString(), token);
        state.MaxBlockHeightWithUpdatedFinalizationTime = finalizedBlock.BlockHeight;
        await _importStateController.SaveChanges(state);
        
        _logger.Debug($"Done processing {JobName}");
    }

    public string GetUniqueIdentifier() => JobName;

    public bool ShouldNodeImportAwait() => true;
}
