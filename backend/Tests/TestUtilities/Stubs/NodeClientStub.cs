using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Stubs;

public class NodeClientStub : INodeClient
{
    public Task SendTransactionAsync(byte[] payload, uint networkId = 100)
    {
        return Task.CompletedTask;
    }

    public Task<NextAccountNonceResponse> GetNextAccountNonceAsync(AccountAddress address)
    {
        return Task.FromResult(new NextAccountNonceResponse(new Nonce(1), true));
    }

    public Task<TransactionStatus> GetTransactionStatusAsync(TransactionHash transactionHash)
    {
        return Task.FromResult(new TransactionStatus { Status = TransactionStatusType.Received });
    }
}