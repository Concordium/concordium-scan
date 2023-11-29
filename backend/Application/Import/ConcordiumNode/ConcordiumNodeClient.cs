using System.Threading;
using System.Threading.Tasks;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;

namespace Application.Import.ConcordiumNode;

/// <summary>
/// Interface which is just a wrapper around <see cref="ConcordiumClient"/>.
/// </summary>
public interface IConcordiumNodeClient
{
    Task<ITransactionStatus> GetBlockItemStatusAsync(TransactionHash transactionHash, CancellationToken token = default);
    
    Task<BakerPoolStatus> GetPoolInfoAsync(BakerId bakerId, IBlockHashInput blockHashInput,
        CancellationToken token = default);
}

internal sealed class ConcordiumNodeClient : IConcordiumNodeClient
{
    private readonly ConcordiumClient _client;

    public ConcordiumNodeClient(ConcordiumClient client)
    {
        _client = client;
    }

    public async Task<BakerPoolStatus> GetPoolInfoAsync(BakerId bakerId, IBlockHashInput blockHashInput, CancellationToken token = default)
    {
        var poolInfoAsync = await _client.GetPoolInfoAsync(bakerId, blockHashInput, token);
        return poolInfoAsync.Response;
    }

    public Task<ITransactionStatus> GetBlockItemStatusAsync(TransactionHash transactionHash, CancellationToken token = default) => 
        _client.GetBlockItemStatusAsync(transactionHash, token);
}
