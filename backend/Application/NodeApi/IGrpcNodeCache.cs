using System.Threading.Tasks;

namespace Application.NodeApi;

public interface IGrpcNodeCache
{
    Task<string> GetOrCreateBlockSummaryAsync(string blockHash, Func<Task<string>> factory);
}