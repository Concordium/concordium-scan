using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using Application.Common.Diagnostics;
using Application.Database;
using Application.NodeApi;
using Microsoft.Extensions.Hosting;

namespace Application.Import.ConcordiumNode;

public class NodeCache : BackgroundService, IGrpcNodeCache
{
    private readonly NodeCacheRepository _repository;
    private readonly Channel<CacheItem> _writeChannel;

    public NodeCache(DatabaseSettings dbSettings, IMetrics metrics)
    {
        _repository = new NodeCacheRepository(dbSettings, metrics);
        var options = new BoundedChannelOptions(100)
        {
            FullMode = BoundedChannelFullMode.DropWrite
        };
        _writeChannel = Channel.CreateBounded<CacheItem>(options);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (var item in _writeChannel.Reader.ReadAllAsync(stoppingToken))
            _repository.WriteBlockSummary(item.Key, item.Content);
    }

    public async Task<string> GetOrCreateBlockSummaryAsync(string blockHash, Func<Task<string>> factory)
    {
        var result = _repository.ReadBlockSummary(blockHash);
        if (result != null)
            return result;
        
        result = await factory();
        var cacheItem = new CacheItem(blockHash, result);
        await _writeChannel.Writer.WriteAsync(cacheItem);
        return result;
    }

    private record CacheItem(string Key, string Content);
}