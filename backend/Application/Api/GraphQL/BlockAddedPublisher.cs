using System.Threading;
using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.FeatureFlags;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

namespace Application.Api.GraphQL;

/// <summary>
/// This publisher is only intended to run, when import from node is disabled.
/// 
/// this is to support a setup where one backend container is handling the import and 
/// one or more backend containers take care of the query part. This handler makes
/// sure that subscriptions actually work in the query containers.
///
/// When import is enabled, new blocks will be published to subscribers as they are imported.
/// </summary>
public class BlockAddedPublisher : BackgroundService
{
    private readonly IFeatureFlags _featureFlags;
    private readonly ITopicEventSender _sender;
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;
    private Block? _latestBlock;
    private readonly ILogger _logger;

    public BlockAddedPublisher(IFeatureFlags featureFlags, ITopicEventSender sender, IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _featureFlags = featureFlags;
        _sender = sender;
        _dbContextFactory = dbContextFactory;
        _logger = Log.ForContext(GetType());
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_featureFlags.ConcordiumNodeImportEnabled)
            return;

        try
        {
            await Run(stoppingToken);
        }
        finally
        {
            _logger.Information("Stopped");
        }
    }

    private async Task Run(CancellationToken stoppingToken)
    {
        await Initialize(stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);

            if (_latestBlock != null)
            {
                await PublishNewBlocks(stoppingToken);
            }
            else
            {
                await Initialize(stoppingToken);
                if (_latestBlock != null)
                    await Publish(_latestBlock, stoppingToken);
            }
        }
    }

    private async Task Initialize(CancellationToken stoppingToken)
    {
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(stoppingToken);
        _latestBlock = await dbContext.Blocks.OrderByDescending(x => x.Id).FirstOrDefaultAsync(stoppingToken);
    }

    private async Task PublishNewBlocks(CancellationToken stoppingToken)
    {
        if (_latestBlock == null) throw new InvalidOperationException("Latest block must not be null.");
        
        await using var dbContext = await _dbContextFactory.CreateDbContextAsync(stoppingToken);
        
        var more = true;
        while (more)
        {
            var blockBatch = await dbContext.Blocks
                .OrderBy(x => x.Id)
                .Where(x => x.Id > _latestBlock.Id)
                .Take(10)
                .ToArrayAsync(stoppingToken);

            if (blockBatch.Length > 0)
            {
                foreach (var block in blockBatch)
                    await Publish(block, stoppingToken);
                _latestBlock = blockBatch.Last();
            }

            more = blockBatch.Length == 10;
        }
    }

    private async Task Publish(Block block, CancellationToken stoppingToken)
    {
        await _sender.SendAsync(nameof(Subscription.BlockAdded), block, stoppingToken);
    }
}