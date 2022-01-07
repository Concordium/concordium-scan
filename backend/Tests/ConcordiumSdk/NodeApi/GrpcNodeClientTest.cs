using System.Net.Http;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
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
    private GrpcNodeClient _target;

    public GrpcNodeClientTest()
    {
        _httpClient = new HttpClient();
        _grpcNodeClientSettings = new GrpcNodeClientSettings()
        {
            Address = "http://ftbccscandevnode.northeurope.cloudapp.azure.com:10111",
            AuthenticationToken = "FTBgrpc2021"
        };
        _target = new GrpcNodeClient(_grpcNodeClientSettings, _httpClient);
    }

    public void Dispose()
    {
        _httpClient.Dispose();
    }

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetConsensusStatusAsync()
    {
        var result = await _target.GetConsensusStatusAsync();
        Assert.NotNull(result);
    }
    
    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetTransactionStatusAsync_KnownFinalizedBlock()
    {
        var transactionHash = new TransactionHash("e2df806768b6f6a52f8654a12be2e6c832fedabe1d1a27eb278dc4e5f9d8631f");
        var result = await _target.GetTransactionStatusAsync(transactionHash);
        Assert.Equal(TransactionStatusType.Finalized, result.Status);
    }

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetBlockSummary()
    {
        var blockHash = new BlockHash("23B6A71B435D1AC0C5B2F7DB8493E5956533B5026F88250056567644D5966FFF");
        var result = await _target.GetBlockSummaryAsync(blockHash);
        Assert.NotNull(result);
    }
}