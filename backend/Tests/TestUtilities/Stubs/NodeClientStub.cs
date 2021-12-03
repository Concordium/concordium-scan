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

    public Task<BlockHash[]> GetBlocksAtHeightAsync(ulong blockHeight)
    {
        return Task.FromResult(new BlockHash[] { new("4b39a13d326f422c76f12e20958a90a4af60a2b7e098b2a59d21d402fff44bfc") });
    }

    public Task<ConsensusStatus> GetConsensusStatusAsync()
    {
        throw new NotImplementedException();
    }
}