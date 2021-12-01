using System.Threading.Tasks;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi;

public interface INodeClient
{
    Task SendTransactionAsync(byte[] payload, uint networkId = 100);

    /// <summary>
    /// Return the best guess as to what the next account nonce should be.
    /// If all account transactions are finalized then this information is reliable.
    /// Otherwise this is the best guess, assuming all other transactions will be committed to blocks and eventually finalized.
    /// </summary>
    Task<NextAccountNonceResponse> GetNextAccountNonceAsync(AccountAddress address);

    Task<TransactionStatus> GetTransactionStatusAsync(TransactionHash transactionHash);
}