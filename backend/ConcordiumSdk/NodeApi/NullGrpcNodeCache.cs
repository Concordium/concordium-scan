using System.Threading.Tasks;

namespace ConcordiumSdk.NodeApi;

public class NullGrpcNodeCache : IGrpcNodeCache
{
    public Task<string> GetOrCreateBlockSummaryAsync(string blockHash, Func<Task<string>> factory)
    {
        return factory();
    }
}