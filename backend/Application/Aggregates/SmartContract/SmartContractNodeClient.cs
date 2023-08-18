using System.Threading;
using System.Threading.Tasks;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;

namespace Application.Aggregates.SmartContract;

public interface ISmartContractNodeClient
{
    Task<QueryResponse<BlockInfo>> GetBlockInfoAsync(IBlockHashInput input, CancellationToken token);
    Task<QueryResponse<IAsyncEnumerable<BlockItemSummary>>> GetBlockTransactionEvents(IBlockHashInput input, CancellationToken token);
    Task<ConsensusInfo> GetConsensusInfoAsync(CancellationToken token);
}

internal sealed class SmartContractNodeClient : ISmartContractNodeClient
{
    private readonly ConcordiumClient _client;

    public SmartContractNodeClient(ConcordiumClient client)
    {
        _client = client;
    }
    
    public Task<QueryResponse<BlockInfo>> GetBlockInfoAsync(IBlockHashInput input, CancellationToken token)
    {
        return _client.GetBlockInfoAsync(input, token);
    }

    public Task<QueryResponse<IAsyncEnumerable<BlockItemSummary>>> GetBlockTransactionEvents(IBlockHashInput input, CancellationToken token)
    {
        return _client.GetBlockTransactionEvents(input, token);
    }

    public Task<ConsensusInfo> GetConsensusInfoAsync(CancellationToken token)
    {
        return _client.GetConsensusInfoAsync(token);
    }
}