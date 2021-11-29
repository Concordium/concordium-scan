using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Net.Client;

namespace Concordium.NodeApi;

public class GrpcClient : IDisposable
{
    private readonly P2P.P2PClient _client;
    private readonly Metadata _metadata;
    private readonly GrpcChannel _grpcChannel;
    private readonly JsonSerializerOptions _jsonSerializerOptions;

    public GrpcClient(GrpcClientSettings settings, HttpClient httpClient)
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
        
        _jsonSerializerOptions = new JsonSerializerOptions()
        {
            PropertyNameCaseInsensitive = true,
        };
        _jsonSerializerOptions.Converters.Add(new SpecialEventJsonConverter());
        _jsonSerializerOptions.Converters.Add(new BlockHashConverter());
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

    public async Task<string> GetAccountInfoAsync(string accountAddress, BlockHash blockHash)
    {
        var request = new GetAddressInfoRequest()
        {
            Address = accountAddress,
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

    public async Task<string> GetTransactionStatusAsync(string transactionHash)
    {
        var request = new TransactionHash { TransactionHash_ = transactionHash };
        var call = _client.GetTransactionStatusAsync(request, CreateCallOptions());
        var response = await call;
        return response.Value;
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
}