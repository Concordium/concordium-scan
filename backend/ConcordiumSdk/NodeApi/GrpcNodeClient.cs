using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Concordium;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
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
    private readonly IGrpcNodeCache _cache;

    public GrpcNodeClient(GrpcNodeClientSettings settings, HttpClient httpClient) : this(settings, httpClient, new NullGrpcNodeCache())
    {
    }

    public GrpcNodeClient(GrpcNodeClientSettings settings, HttpClient httpClient, IGrpcNodeCache cache)
    {
        _metadata = new Metadata
        {
            { "authentication", settings.AuthenticationToken }
        };

        var options = new GrpcChannelOptions
        {
            Credentials = ChannelCredentials.Insecure,
            HttpClient = httpClient,
            DisposeHttpClient = false,
            MaxReceiveMessageSize = 64 * 1024 * 1024, // 64 MB
        };
        _grpcChannel = GrpcChannel.ForAddress(settings.Address, options);
        _client = new P2P.P2PClient(_grpcChannel);

        _jsonSerializerOptions = GrpcNodeJsonSerializerOptionsFactory.Create();
        _cache = cache;
    }

    public async Task<ConsensusStatus> GetConsensusStatusAsync(CancellationToken cancellationToken = default)
    {
        var callOptions = CreateCallOptions(cancellationToken);
        var call = _client.GetConsensusStatusAsync(new Empty(), callOptions);
        var response = await call;
        return JsonSerializer.Deserialize<ConsensusStatus>(response.Value, _jsonSerializerOptions);
    }

    public async Task<BlockHash[]> GetBlocksAtHeightAsync(ulong blockHeight, CancellationToken cancellationToken = default)
    {
        var request = new BlockHeight()
        {
            BlockHeight_ = blockHeight,
            RestrictToGenesisIndex = false
        };

        var call = _client.GetBlocksAtHeightAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        return JsonSerializer.Deserialize<BlockHash[]>(response.Value, _jsonSerializerOptions);
    }

    public async Task<ConcordiumSdk.Types.AccountAddress[]> GetAccountListAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var callOptions = CreateCallOptions(cancellationToken, TimeSpan.FromSeconds(60));
        var call = _client.GetAccountListAsync(request, callOptions);
        var response = await call;
        return JsonSerializer.Deserialize<ConcordiumSdk.Types.AccountAddress[]>(response.Value, _jsonSerializerOptions)!;
    }
    
    public async Task<BlockInfo> GetBlockInfoAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var call = _client.GetBlockInfoAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        
        return JsonSerializer.Deserialize<BlockInfo>(response.Value, _jsonSerializerOptions);
    }

    public async Task<string> GetBlockSummaryStringAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var result = await _cache.GetOrCreateBlockSummaryAsync(blockHash.AsString, async () =>
        {
            var request = new Concordium.BlockHash
            {
                BlockHash_ = blockHash.AsString
            };

            var call = _client.GetBlockSummaryAsync(request, CreateCallOptions(cancellationToken));
            var response = await call;
            return response.Value;
        });
        
        return result;
    }

    public async Task<BlockSummaryBase> GetBlockSummaryAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var stringResponse = await GetBlockSummaryStringAsync(blockHash, cancellationToken);
        var result = JsonSerializer.Deserialize<BlockSummaryBase>(stringResponse, _jsonSerializerOptions)!;
        return result;
    }

    public async Task<AccountInfo> GetAccountInfoAsync(ConcordiumSdk.Types.AccountAddress accountAddress, BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new GetAddressInfoRequest()
        {
            Address = accountAddress.AsString,
            BlockHash = blockHash.AsString
        };
        var call = _client.GetAccountInfoAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<AccountInfo>(response.Value, _jsonSerializerOptions)!;
        return result;
    }

    public async Task<RewardStatusBase> GetRewardStatusAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };

        var call = _client.GetRewardStatusAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<RewardStatusBase>(response.Value, _jsonSerializerOptions)!;
        return result;
    }

    public async Task<string> GetAccountInfoStringAsync(ConcordiumSdk.Types.AccountAddress accountAddress, BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new GetAddressInfoRequest()
        {
            Address = accountAddress.AsString,
            BlockHash = blockHash.AsString
        };
        var call = _client.GetAccountInfoAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        return response.Value;
    }

    public async Task<PeerListResponse> PeerListAsync(bool includeBootstrappers = false, CancellationToken cancellationToken = default)
    {
        var request = new PeersRequest
        {
            IncludeBootstrappers = includeBootstrappers
        };

        var callOptions = CreateCallOptions(cancellationToken);
        var call = _client.PeerListAsync(request, callOptions);
        var response = await call;
        return response;
    }

    public async Task<PeerStatsResponse> PeerStatsAsync(bool includeBootstrappers = false, CancellationToken cancellationToken = default)
    {
        var request = new PeersRequest
        {
            IncludeBootstrappers = includeBootstrappers
        };

        var callOptions = CreateCallOptions(cancellationToken);
        var call = _client.PeerStatsAsync(request, callOptions);
        var response = await call;
        return response;
    }

    public async Task<TransactionStatus> GetTransactionStatusAsync(ConcordiumSdk.Types.TransactionHash transactionHash, CancellationToken cancellationToken = default)
    {
        var request = new TransactionHash { TransactionHash_ = transactionHash.AsString };
        var call = _client.GetTransactionStatusAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<TransactionStatus>(response.Value, _jsonSerializerOptions);
        return result;
    }

    public async Task<string> GetTransactionStatusInBlockAsync(string transactionHash, BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new GetTransactionStatusInBlockRequest()
        {
            TransactionHash = transactionHash,
            BlockHash = blockHash.AsString
        };
        var call = _client.GetTransactionStatusInBlockAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        return response.Value;
    }

    private CallOptions CreateCallOptions(CancellationToken cancellationToken)
    {
        return CreateCallOptions(cancellationToken, TimeSpan.FromSeconds(30));
    }
    
    private CallOptions CreateCallOptions(CancellationToken cancellationToken, TimeSpan timeout)
    {
        var deadline = DateTime.UtcNow + timeout;
        return new CallOptions(_metadata, deadline, cancellationToken);
    }

    public void Dispose()
    {
        _grpcChannel?.Dispose();
    }

    public async Task SendTransactionAsync(byte[] payload, uint networkId = 100, CancellationToken cancellationToken = default)
    {
        var request = new SendTransactionRequest
        {
            NetworkId = networkId,
            Payload = ByteString.CopyFrom(payload)
        };
        var call = _client.SendTransactionAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        if (!response.Value)
            throw new InvalidOperationException("Response indicated that transaction was not successfully sent.");
    }

    /// <summary>
    /// Return the best guess as to what the next account nonce should be.
    /// If all account transactions are finalized then this information is reliable.
    /// Otherwise this is the best guess, assuming all other transactions will be committed to blocks and eventually finalized.
    /// </summary>
    public async Task<NextAccountNonceResponse> GetNextAccountNonceAsync(ConcordiumSdk.Types.AccountAddress address, CancellationToken cancellationToken = default)
    {
        var request = new AccountAddress
        {
            AccountAddress_ = address.AsString
        };
        var call = _client.GetNextAccountNonceAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<NextAccountNonceResponse>(response.Value, _jsonSerializerOptions);
        if (result == null) throw new InvalidOperationException("Deserialization unexpectedly returned null!");
        return result;
    }

    public async Task<IdentityProviderInfo[]> GetIdentityProvidersAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var call = _client.GetIdentityProvidersAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<IdentityProviderInfo[]>(response.Value, _jsonSerializerOptions);
        if (result == null) throw new InvalidOperationException("Deserialization unexpectedly returned null!");
        return result;
    }

    public async Task<ContractAddress[]> GetInstancesAsync(BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new Concordium.BlockHash
        {
            BlockHash_ = blockHash.AsString
        };
        var call = _client.GetInstancesAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<ContractAddress[]>(response.Value, _jsonSerializerOptions);
        if (result == null) throw new InvalidOperationException("Deserialization unexpectedly returned null!");
        return result;
    }
    
    public async Task<ContractInstanceInfo> GetInstanceInfoAsync(ContractAddress contractAddress, BlockHash blockHash, CancellationToken cancellationToken = default)
    {
        var request = new GetAddressInfoRequest
        {
            Address = JsonSerializer.Serialize(contractAddress, _jsonSerializerOptions),
            BlockHash = blockHash.AsString 
        };
        var call = _client.GetInstanceInfoAsync(request, CreateCallOptions(cancellationToken));
        var response = await call;
        var result = JsonSerializer.Deserialize<ContractInstanceInfo>(response.Value, _jsonSerializerOptions);
        if (result == null) throw new InvalidOperationException("Deserialization unexpectedly returned null!");
        return result;
    }

    public async Task<PeerVersion> GetPeerVersionAsync()
    {
        var result = await _client.PeerVersionAsync(new Empty(), CreateCallOptions(CancellationToken.None));
        if (result == null) throw new InvalidOperationException("Unexpectedly received null from rpc operation.");
        return PeerVersion.Parse(result.Value);
    }
}