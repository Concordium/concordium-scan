using System.Net.Http;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Types;

namespace Tests.ConcordiumSdk.NodeApi;

/// <summary>
/// All tests in this class is intended for manually running tests with the client code against a real running Concordium Node.
/// Make sure the settings are correct before running a test
/// All tests are intentionally set to be skipped.
/// </summary>
public class GrpcNodeClientTest : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly GrpcNodeClientSettings _grpcNodeClientSettings;

    public GrpcNodeClientTest()
    {
        _httpClient = new HttpClient();
        _grpcNodeClientSettings = new GrpcNodeClientSettings()
        {
            Address = "http://40.127.163.29:10000",
            AuthenticationToken = "FTBgrpc2021"
        };
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetTransactionStatusAsync_KnownFinalizedBlock()
    {
        var target = new GrpcNodeClient(_grpcNodeClientSettings, _httpClient);
        var transactionHash = new TransactionHash("e2df806768b6f6a52f8654a12be2e6c832fedabe1d1a27eb278dc4e5f9d8631f");
        var result = await target.GetTransactionStatusAsync(transactionHash);
        Assert.Equal(TransactionStatusType.Finalized, result.Status);
    }
}