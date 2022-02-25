using System.Net.Http;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using FluentAssertions;
using Tests.TestUtilities;

namespace Tests.ConcordiumSdk.NodeApi;

/// <summary>
/// All tests in this class is intended for manually running tests with the client code against a real running Concordium Node.
/// Make sure the settings are correct before running a test
/// All tests are intentionally set to be skipped.
/// </summary>
public class GrpcNodeClientTest : IDisposable
{
    private readonly HttpClient _httpClient;
    private readonly GrpcNodeClient _target;

    public GrpcNodeClientTest()
    {
        _httpClient = new HttpClient();
        var grpcNodeClientSettings = new GrpcNodeClientSettings()
        {
            Address = "http://dev-ccdscan-vm.northeurope.cloudapp.azure.com:10000",
            AuthenticationToken = "test-ccnode-auth-token"
        };
        _target = new GrpcNodeClient(grpcNodeClientSettings, _httpClient);
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

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetAccountList()
    {
        var blockHash = new BlockHash("90dd7b925d9cf5c26de7852b169ab806fba509406809d5a36d46b30810d09b44");
        var result = await _target.GetAccountListAsync(blockHash);
        Assert.NotNull(result);
    }
    
    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetAccountInfo()
    {
        var blockHashes = await _target.GetBlocksAtHeightAsync(1928019);
        var address = new AccountAddress("32jTsKCtpGKr56WLweiPJB6jvLoCgFAHvfXtexHeoovJWu2PBD");
        var result = await _target.GetAccountInfoAsync(address, blockHashes.Single());
        result.Should().NotBeNull();
        result.AccountNonce.Should().Be(new Nonce(29));
        result.AccountAmount.Should().Be(CcdAmount.FromMicroCcd(56157825516550));
        result.AccountAddress.Should().Be(address);
    }

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetAccountInfoString()
    {
        var blockHashes = await _target.GetBlocksAtHeightAsync(1928019);
        var address = new AccountAddress("4acpJCLj2Q56s7gPDXAySrELFrg6g9wypTH43jCzq1gXGnWaty");
        var result = await _target.GetAccountInfoStringAsync(address, blockHashes.Single());
        Assert.NotNull(result);
    }

    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task GetIdentityProvidersAsync()
    {
        var blockHashes = await _target.GetBlocksAtHeightAsync(2224396);
        var result = await _target.GetIdentityProvidersAsync(blockHashes.Single());
        result.Should().NotBeNull();
    }
    
    [Fact(Skip = "Intentionally skipped. Intended for manual integration test.")]
    public async Task HarvestAccounts()
    {
        var databaseFixture = new DatabaseFixture();

        var blockHashes = await _target.GetBlocksAtHeightAsync(2);
        var blockHash = blockHashes.Single();
        
        var accountAddresses = await _target.GetAccountListAsync(blockHash);
        foreach (var accountAddress in accountAddresses)
        {
            var result = await _target.GetAccountInfoStringAsync(accountAddress, blockHash);
            var param = new
            {
                Data = result,
                HarvestName = "all-height-2"
            };
            await using var connection = databaseFixture.GetOpenConnection();
            await connection.ExecuteAsync("insert into raw_account_harvest (data, harvest_name) values (CAST(@Data AS json), @HarvestName)", param);
        }
    }
}