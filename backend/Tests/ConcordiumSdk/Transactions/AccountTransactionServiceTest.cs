using System.Net.Http;
using ConcordiumSdk.ExportedMobileWalletFile;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.Transactions;
using ConcordiumSdk.Types;
using Tests.TestUtilities.Stubs;
using Xunit.Abstractions;

namespace Tests.ConcordiumSdk.Transactions;

/// <summary>
/// Tests in this suite can be run both with stubs (no transactions will actually happen) and with a real client
/// and signer (will place actual transactions on a Concordium Node). Default is to run with stubs.
///
/// To use real client and signer you need to modify the constructor and enter valid values for both Node-settings
/// and exported wallet file.
/// </summary>
public class AccountTransactionServiceTest : IDisposable
{
    private readonly ITestOutputHelper _output;
    private readonly AccountTransactionService _target;
    private readonly AccountAddress _senderAddress;
    private readonly INodeClient _nodeClient;
    private readonly ITransactionSigner _signer;
    private readonly HttpClient? _httpClient;

    public AccountTransactionServiceTest(ITestOutputHelper output)
    {
        _output = output;
        _senderAddress = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd");

        if (true)
        {
            _nodeClient = new NodeClientStub();
            _signer = new TransactionSignerStub();
        }
        else
        {
            var grpcClientSettings = new GrpcNodeClientSettings
            {
                Address = "http://<your-node-ip-address>:10000",
                AuthenticationToken = "your-auth-token"
            };
            var exportedWalletFilePath = @"c:\temp\export.concordiumwallet";
            var exportedWalletPassword = "your-password";

            _httpClient = new HttpClient();
            _nodeClient = new GrpcNodeClient(grpcClientSettings, _httpClient);

            var mobileWalletExport = MobileWalletExportReader.ReadAndDecrypt(exportedWalletFilePath, exportedWalletPassword);
            var privateKeyAsHexString = mobileWalletExport.GetSingleSignKeyForAccountWithAddress(_senderAddress);
            
            _signer = new Ed25519TransactionSigner(privateKeyAsHexString);
        }

        _target = new AccountTransactionService(_nodeClient);
    }

    public void Dispose()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task SendAccountTransactionAsync_SingleTransfer()
    {
        var toAddress = new AccountAddress("3uHj5LudVeJMZ7xAm3E4bbFwN61N4ijb9KtnAuARkhoAMLiNYa");
        var amount = CcdAmount.FromCcd(100);
        var payload = new SimpleTransferPayload(amount, toAddress);

        var txHash = await _target.SendAccountTransactionAsync(_senderAddress, payload, _signer);

        Assert.Equal(32, txHash.AsBytes.Length);

        _output.WriteLine($"txHash1: {txHash.AsString}");
    }
    
    [Fact]
    public async Task SendAccountTransactionAsync_SingleTransferWithMemo()
    {
        var toAddress = new AccountAddress("3uHj5LudVeJMZ7xAm3E4bbFwN61N4ijb9KtnAuARkhoAMLiNYa");
        var amount = CcdAmount.FromCcd(2);
        var payload = new SimpleTransferWithMemoPayload(amount, toAddress, Memo.CreateCborEncodedFromText("Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."));

        var txHash = await _target.SendAccountTransactionAsync(_senderAddress, payload, _signer);

        Assert.Equal(32, txHash.AsBytes.Length);

        _output.WriteLine($"txHash1: {txHash.AsString}");
    }

    /// <summary>
    /// When making multiple transfers from the same sender account you can  control the nonce value yourself
    /// like shown in this example. This results in one less rpc-operation to the Concordium node thus speeding
    /// up the transfers
    /// </summary>
    [Fact]
    public async Task SendAccountTransactionAsync_MultipleTransferFromSameSender()
    {
        var amount = CcdAmount.FromCcd(100);
        var toAddress1 = new AccountAddress("3uHj5LudVeJMZ7xAm3E4bbFwN61N4ijb9KtnAuARkhoAMLiNYa");
        var toAddress2 = new AccountAddress("4hT1SmAHGbRH5m5UocN6xhUv9SXcs7HbNUPHqhS8Zy5jKubU1J");
        var toAddress3 = new AccountAddress("3itG8CR6uRhvdSZtxN1DWvyPdmHsdBpMjic4GFrpqebcWZAKwU");

        var nextAccountNonceResponse = await _nodeClient.GetNextAccountNonceAsync(_senderAddress);
        
        var nextAccountNonce = nextAccountNonceResponse.Nonce;
        var txHash1 = await _target.SendAccountTransactionAsync(_senderAddress, nextAccountNonce, new SimpleTransferPayload(amount, toAddress1), _signer);

        nextAccountNonce = nextAccountNonce.Increment();
        var txHash2 = await _target.SendAccountTransactionAsync(_senderAddress, nextAccountNonce, new SimpleTransferPayload(amount, toAddress2), _signer);
        
        nextAccountNonce = nextAccountNonce.Increment();
        var txHash3 = await _target.SendAccountTransactionAsync(_senderAddress, nextAccountNonce, new SimpleTransferPayload(amount, toAddress3), _signer);

        _output.WriteLine($"txHash1: {txHash1.AsString}");
        _output.WriteLine($"txHash2: {txHash2.AsString}");
        _output.WriteLine($"txHash3: {txHash3.AsString}");
        
        Assert.Equal(32, txHash1.AsBytes.Length);
        Assert.Equal(32, txHash2.AsBytes.Length);
        Assert.Equal(32, txHash3.AsBytes.Length);
    }
}