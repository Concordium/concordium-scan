using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concordium;
using ConcordiumSdk.NodeApi.Types;
using Google.Protobuf;
using Grpc.Core;
using Grpc.Net.Client;
using AccountAddress = Concordium.AccountAddress;
using BlockHash = ConcordiumSdk.Types.BlockHash;
using TransactionHash = Concordium.TransactionHash;

namespace ConcordiumSdk.NodeApi;

public class GrpcNodeClient : INodeClient, IDisposable
{
    private readonly P2P.P2PClient _client;
    private readonly Metadata _metadata;
    private readonly GrpcChannel _grpcChannel;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GrpcNodeClient(GrpcNodeClientSettings settings, HttpClient httpClient)
    {
        _metadata = new Metadata
        {
            { "authentication", settings.AuthenticationToken }
        };

        var options = new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Insecure,
            HttpClient = httpClient,
            DisposeHttpClient = false
        };
        _grpcChannel = GrpcChannel.ForAddress(settings.Address, options);
        _client = new P2P.P2PClient(_grpcChannel);

        _jsonSerializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
    }

    public async Task<ConsensusStatus> GetConsensusStatusAsync()
    {
        var callOptions = CreateCallOptions();
        var call = _client.GetConsensusStatusAsync(new Empty(), callOptions);
        var response = await call;
        return JsonSerializer.Deserialize<ConsensusStatus>(response.Value, _jsonSerializerOptions);
    }

    public async Task<BlockHash[]> GetBlocksAtHeightAsync(ulong blockHeight)
    {
        var request = new BlockHeight()
        {
            BlockHeight_ = blockHeight,
            RestrictToGenesisIndex = false
        };

        var call = _client.GetBlocksAtHeightAsync(request, CreateCallOptions());
        var response = await call;
        return JsonSerializer.Deserialize<BlockHash[]>(response.Value, _jsonSerializerOptions);
    }

    public async Task<ConcordiumSdk.Types.AccountAddress[]> GetAccountListAsync(BlockHash blockHash)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var call = _client.GetAccountListAsync(request, CreateCallOptions());
        var response = await call;
        return JsonSerializer.Deserialize<ConcordiumSdk.Types.AccountAddress[]>(response.Value, _jsonSerializerOptions)!;
    }
    
    public async Task<BlockInfo> GetBlockInfoAsync(BlockHash blockHash)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var call = _client.GetBlockInfoAsync(request, CreateCallOptions());
        var response = await call;
        
        return JsonSerializer.Deserialize<BlockInfo>(response.Value, _jsonSerializerOptions);
    }

    public async Task<string> GetBlockSummaryStringAsync(BlockHash blockHash)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        
        var call = _client.GetBlockSummaryAsync(request, CreateCallOptions());
        var response = await call;
        return response.Value;
    }
    
    public async Task<BlockSummary> GetBlockSummaryAsync(BlockHash blockHash)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var call = _client.GetBlockSummaryAsync(request, CreateCallOptions());
        var response = await call;
        var result = JsonSerializer.Deserialize<BlockSummary>(response.Value, _jsonSerializerOptions);
        return result;
    }

    public async Task<AccountInfo> GetAccountInfoAsync(ConcordiumSdk.Types.AccountAddress accountAddress, BlockHash blockHash)
    {
        var request = new GetAddressInfoRequest()
        {
            Address = accountAddress.AsString,
            BlockHash = blockHash.AsString
        };
        var call = _client.GetAccountInfoAsync(request, CreateCallOptions());
        var response = await call;
        var result = JsonSerializer.Deserialize<AccountInfo>(response.Value, _jsonSerializerOptions)!;
        return result;
    }

    public async Task<string> GetAccountInfoStringAsync(ConcordiumSdk.Types.AccountAddress accountAddress, BlockHash blockHash)
    {
        var request = new GetAddressInfoRequest()
        {
            Address = accountAddress.AsString,
            BlockHash = blockHash.AsString
        };
        var call = _client.GetAccountInfoAsync(request, CreateCallOptions());
        var response = await call;
        return response.Value;
    }

    public async Task<PeerListResponse> PeerListAsync(bool includeBootstrappers = false)
    {
        var request = new PeersRequest
        {
            IncludeBootstrappers = includeBootstrappers
        };

        var callOptions = CreateCallOptions();
        var call = _client.PeerListAsync(request, callOptions);
        var response = await call;
        return response;
    }

    public async Task<PeerStatsResponse> PeerStatsAsync(bool includeBootstrappers = false)
    {
        var request = new PeersRequest
        {
            IncludeBootstrappers = includeBootstrappers
        };

        var callOptions = CreateCallOptions();
        var call = _client.PeerStatsAsync(request, callOptions);
        var response = await call;
        return response;
    }

    public async Task<TransactionStatus> GetTransactionStatusAsync(ConcordiumSdk.Types.TransactionHash transactionHash)
    {
        var request = new TransactionHash { TransactionHash_ = transactionHash.AsString };
        var call = _client.GetTransactionStatusAsync(request, CreateCallOptions());
        var response = await call;
        var result = JsonSerializer.Deserialize<TransactionStatus>(response.Value, _jsonSerializerOptions);
        return result;
    }

    public async Task<string> GetTransactionStatusInBlockAsync(string transactionHash, BlockHash blockHash)
    {
        var request = new GetTransactionStatusInBlockRequest()
        {
            TransactionHash = transactionHash,
            BlockHash = blockHash.AsString
        };
        var call = _client.GetTransactionStatusInBlockAsync(request, CreateCallOptions());
        var response = await call;
        return response.Value;
    }

    private CallOptions CreateCallOptions()
    {
        return new CallOptions(_metadata, DateTime.UtcNow.AddSeconds(30), CancellationToken.None);
    }

    public void Dispose()
    {
        _grpcChannel?.Dispose();
    }

    public async Task SendTransactionAsync(byte[] payload, uint networkId = 100)
    {
        var request = new SendTransactionRequest
        {
            NetworkId = networkId,
            Payload = ByteString.CopyFrom(payload)
        };
        var call = _client.SendTransactionAsync(request, CreateCallOptions());
        var response = await call;
        if (!response.Value)
            throw new InvalidOperationException("Response indicated that transaction was not successfully sent.");
    }

    /// <summary>
    /// Return the best guess as to what the next account nonce should be.
    /// If all account transactions are finalized then this information is reliable.
    /// Otherwise this is the best guess, assuming all other transactions will be committed to blocks and eventually finalized.
    /// </summary>
    public async Task<NextAccountNonceResponse> GetNextAccountNonceAsync(ConcordiumSdk.Types.AccountAddress address)
    {
        var request = new AccountAddress
        {
            AccountAddress_ = address.AsString
        };
        var call = _client.GetNextAccountNonceAsync(request, CreateCallOptions());
        var response = await call;
        var result = JsonSerializer.Deserialize<NextAccountNonceResponse>(response.Value, _jsonSerializerOptions);
        if (result == null) throw new InvalidOperationException("Deserialization unexpectedly returned null!");
        return result;
    }
}