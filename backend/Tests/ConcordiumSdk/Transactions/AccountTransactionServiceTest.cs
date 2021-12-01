using System.Net.Http;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Transactions;
using ConcordiumSdk.Types;
using Tests.TestUtilities.Stubs;

namespace Tests.ConcordiumSdk.Transactions;

public class AccountTransactionServiceTest
{
    [Fact]
    public async Task SendAccountTransactionAsync()
    {
        var nodeClient = CreateFakeNodeClient();
        var target = new AccountTransactionService(nodeClient);
        
        var sender = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");
        var toAddress = new AccountAddress("4hT1SmAHGbRH5m5UocN6xhUv9SXcs7HbNUPHqhS8Zy5jKubU1J");
        var amount = Amount.FromCcd(100);
        var payload = new SimpleTransferPayload(amount, toAddress);

        var signer = new TransactionSignerStub();
        var txHash = await target.SendAccountTransactionAsync(sender, payload, signer);

        Assert.NotNull(txHash);
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