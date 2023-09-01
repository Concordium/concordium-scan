using System.Threading;
using System.Threading.Tasks;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;

namespace Application.Aggregates.Contract;

public interface IContractNodeClient
{
    Task<QueryResponse<BlockInfo>> GetBlockInfoAsync(IBlockHashInput input, CancellationToken token);
    Task<QueryResponse<IAsyncEnumerable<BlockItemSummary>>> GetBlockTransactionEvents(IBlockHashInput input, CancellationToken token);
    Task<ConsensusInfo> GetConsensusInfoAsync(CancellationToken token);
}