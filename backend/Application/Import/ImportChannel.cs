using System.Threading.Channels;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Application.Import;

public class ImportChannel
{
    private readonly Channel<Task<BlockDataEnvelope>> _channel;
    private readonly TaskCompletionSource<InitialImportState> _importStateTaskCompletionSource;

    public ImportChannel()
    {
        // The capacity controls the level of parallelism in the import from the concordium node
        // Currently set to 8, which is cc-node-threads X 1.5
        var options = new BoundedChannelOptions(6)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<Task<BlockDataEnvelope>>(options);
        _importStateTaskCompletionSource = new TaskCompletionSource<InitialImportState>();
    }

    public ChannelWriter<Task<BlockDataEnvelope>> Writer => _channel.Writer;
    public ChannelReader<Task<BlockDataEnvelope>> Reader => _channel.Reader;

    public void SetInitialImportState(InitialImportState initialImportState)
    {
        _importStateTaskCompletionSource.SetResult(initialImportState);
    }

    public Task<InitialImportState> GetInitialImportStateAsync()
    {
        return _importStateTaskCompletionSource.Task;
    }
}

public record InitialImportState(long? MaxBlockHeight, BlockHash? GenesisBlockHash);

public record BlockDataEnvelope(BlockDataPayload Payload);

public record BlockDataPayload
{
    private readonly Func<Task<BakerPoolStatus[]>> _readAllBakerPoolStatusesFunc;

    public BlockDataPayload(BlockInfo blockInfo, 
        BlockSummaryBase blockSummary, 
        AccountInfosRetrieved accountInfos,
        RewardStatusBase rewardStatus,
        Func<Task<BakerPoolStatus[]>> readAllBakerPoolStatusesFunc)
    {
        BlockInfo = blockInfo;
        BlockSummary = blockSummary;
        AccountInfos = accountInfos;
        RewardStatus = rewardStatus;
        _readAllBakerPoolStatusesFunc = readAllBakerPoolStatusesFunc;
    }

    public BlockInfo BlockInfo { get; }
    public BlockSummaryBase BlockSummary { get; }
    public AccountInfosRetrieved AccountInfos { get; }
    public RewardStatusBase RewardStatus { get; }

    public async Task<BakerPoolStatus[]> ReadAllBakerPoolStatuses()
    {
        return await _readAllBakerPoolStatusesFunc();
    }
}

public record GenesisBlockDataPayload : BlockDataPayload
{
    public GenesisBlockDataPayload(BlockInfo blockInfo,
        BlockSummaryBase blockSummary,
        AccountInfosRetrieved accountInfos,
        RewardStatusBase rewardStatus, 
        IdentityProviderInfo[] genesisIdentityProviders,
        Func<Task<BakerPoolStatus[]>> readAllBakerPoolStatusesFunc) 
        : base(blockInfo, blockSummary, accountInfos, rewardStatus, readAllBakerPoolStatusesFunc)
    {
        GenesisIdentityProviders = genesisIdentityProviders;
    }

    public IdentityProviderInfo[] GenesisIdentityProviders { get; init; }
}

public record AccountInfosRetrieved(
    AccountInfo[] CreatedAccounts,
    AccountInfo[] BakersWithNewPendingChanges);