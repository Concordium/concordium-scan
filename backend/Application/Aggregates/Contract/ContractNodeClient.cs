using System.Threading;
using System.Threading.Tasks;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;

namespace Application.Aggregates.Contract;

internal sealed class ContractNodeClient : IContractNodeClient
{
    private readonly ConcordiumClient _client;

    public ContractNodeClient(ConcordiumClient client)
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