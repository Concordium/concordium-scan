using System.Threading.Tasks;

namespace ConcordiumSdk.NodeApi;

public interface IGrpcNodeCache
{
    Task<string> GetOrCreateBlockSummaryAsync(string blockHash, Func<Task<string>> factory);
}