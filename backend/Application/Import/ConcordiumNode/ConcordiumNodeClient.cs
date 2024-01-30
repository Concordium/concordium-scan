using System.Threading;
using System.Threading.Tasks;
using Application.Import.ConcordiumNode.ConcordiumClientWrappers;
using Concordium.Sdk.Client;
using Concordium.Sdk.Types;

namespace Application.Import.ConcordiumNode;

/// <summary>
/// Interface which is just a wrapper around <see cref="ConcordiumClient"/>.
/// </summary>
public interface IConcordiumNodeClient
{
    Task<BlockInfo> GetBlockInfoAsync(IBlockHashInput blockHashInput, CancellationToken token = default);
    
    Task<IBlockItemSummaryWrapper> GetBlockItemStatusAsync(TransactionHash transactionHash, CancellationToken token = default);
    
    Task<BakerPoolStatus> GetPoolInfoAsync(BakerId bakerId, IBlockHashInput blockHashInput,
        CancellationToken token = default);
    
    public Task<AccountInfo> GetAccountInfoAsync(
        IAccountIdentifier accountIdentifier,
        IBlockHashInput blockHash,
        CancellationToken token = default);
}

internal sealed class ConcordiumNodeClient : IConcordiumNodeClient
{
    private readonly ConcordiumClient _client;

    public ConcordiumNodeClient(ConcordiumClient client)
    {
        _client = client;
    }

    public async Task<BlockInfo> GetBlockInfoAsync(IBlockHashInput blockHashInput, CancellationToken token = default)
    {
        var blockInfoAsync = await _client.GetBlockInfoAsync(blockHashInput, token);
        return blockInfoAsync.Response;
    }

    public async Task<BakerPoolStatus> GetPoolInfoAsync(BakerId bakerId, IBlockHashInput blockHashInput, CancellationToken token = default)
    {
        var poolInfoAsync = await _client.GetPoolInfoAsync(bakerId, blockHashInput, token);
        return poolInfoAsync.Response;
    }

    public async Task<AccountInfo> GetAccountInfoAsync(IAccountIdentifier accountIdentifier, IBlockHashInput blockHash,
        CancellationToken token = default)
    {
        var accountInfo = await _client.GetAccountInfoAsync(accountIdentifier, blockHash, token);
        return accountInfo.Response;
    }

    public async Task<IBlockItemSummaryWrapper> GetBlockItemStatusAsync(TransactionHash transactionHash,
        CancellationToken token = default)
    {
        var blockItemSummary = await _client.GetBlockItemStatusAsync(transactionHash, token);
        return new BlockItemSummaryWrapper(blockItemSummary);
    }
}
