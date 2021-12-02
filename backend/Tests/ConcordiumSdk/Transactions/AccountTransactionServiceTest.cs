using System.Net.Http;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Transactions;
using ConcordiumSdk.Types;
using Tests.TestUtilities.Stubs;

namespace Tests.ConcordiumSdk.Transactions;

public class AccountTransactionServiceTest
{
    private readonly INodeClient _nodeClient;
    private readonly AccountTransactionService _target;
    private TransactionSignerStub _signer;

    public AccountTransactionServiceTest()
    {
        _nodeClient = CreateFakeNodeClient();
        _target = new AccountTransactionService(_nodeClient);
        _signer = new TransactionSignerStub();
    }

    [Fact]
    public async Task SendAccountTransactionAsync_SingleTransfer()
    {
        var sender = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        
        var toAddress = new AccountAddress("4hT1SmAHGbRH5m5UocN6xhUv9SXcs7HbNUPHqhS8Zy5jKubU1J");
        var amount = CcdAmount.FromCcd(100);
        var payload = new SimpleTransferPayload(amount, toAddress);

        var txHash = await _target.SendAccountTransactionAsync(sender, payload, _signer);

        Assert.Equal(32, txHash.AsBytes.Length);
    }

    /// <summary>
    /// When making multiple transfers from the same sender account you can  control the nonce value yourself
    /// like shown in this example. This results in one less rpc-operation to the Concordium node thus speeding
    /// up the transfers
    /// </summary>
    [Fact]
    public async Task SendAccountTransactionAsync_MultipleTransferFromSameSender()
    {
        var sender = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        
        var amount = CcdAmount.FromCcd(100);
        var toAddress1 = new AccountAddress("4hT1SmAHGbRH5m5UocN6xhUv9SXcs7HbNUPHqhS8Zy5jKubU1J");
        var toAddress2 = new AccountAddress("4hT1SmAHGbRH5m5UocN6xhUv9SXcs7HbNUPHqhS8Zy5jKubU1J");
        var toAddress3 = new AccountAddress("4hT1SmAHGbRH5m5UocN6xhUv9SXcs7HbNUPHqhS8Zy5jKubU1J");

        var nextAccountNonceResponse = await _nodeClient.GetNextAccountNonceAsync(sender);
        
        var nextAccountNonce = nextAccountNonceResponse.Nonce;
        var txHash1 = await _target.SendAccountTransactionAsync(sender, nextAccountNonce, new SimpleTransferPayload(amount, toAddress1), _signer);

        nextAccountNonce = nextAccountNonce.Increment();
        var txHash2 = await _target.SendAccountTransactionAsync(sender, nextAccountNonce, new SimpleTransferPayload(amount, toAddress2), _signer);
        
        nextAccountNonce = nextAccountNonce.Increment();
        var txHash3 = await _target.SendAccountTransactionAsync(sender, nextAccountNonce, new SimpleTransferPayload(amount, toAddress3), _signer);

        Assert.Equal(32, txHash1.AsBytes.Length);
        Assert.Equal(32, txHash2.AsBytes.Length);
        Assert.Equal(32, txHash3.AsBytes.Length);
    }

    private INodeClient CreateFakeNodeClient()
    {
        return new NodeClientStub();
    }

    private static INodeClient CreateRealNodeClient()
    {
        using var httpClient = new HttpClient();
        var grpcClientSettings = new GrpcNodeClientSettings
        {
            Address = "http://40.127.163.29:10000",
            AuthenticationToken = "FTBgrpc2021"
        };
        var grpcClient = new GrpcNodeClient(grpcClientSettings, httpClient);
        return grpcClient;
    }
}