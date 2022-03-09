using System.Threading;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

public interface INodeClient
{
    Task SendTransactionAsync(byte[] payload, uint networkId = 100, CancellationToken cancellationToken = default);

    /// <summary>
    /// Return the best guess as to what the next account nonce should be.
    /// If all account transactions are finalized then this information is reliable.
    /// Otherwise this is the best guess, assuming all other transactions will be committed to blocks and eventually finalized.
    /// </summary>
    Task<NextAccountNonceResponse> GetNextAccountNonceAsync(AccountAddress address, CancellationToken cancellationToken = default);

    Task<TransactionStatus> GetTransactionStatusAsync(TransactionHash transactionHash, CancellationToken cancellationToken = default);
    Task<BlockHash[]> GetBlocksAtHeightAsync(ulong blockHeight, CancellationToken cancellationToken = default);
    Task<ConsensusStatus> GetConsensusStatusAsync(CancellationToken cancellationToken = default);
}