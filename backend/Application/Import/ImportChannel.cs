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

public record BlockDataEnvelope(BlockDataPayload Payload, ConsensusStatus ConsensusStatus);

public record BlockDataPayload
{
    private readonly Func<Task<BakerPoolStatus[]>> _readAllBakerPoolStatusesFunc;
    private readonly Func<Task<PoolStatusPassiveDelegation>> _passiveDelegationPoolStatusFunc;

    public BlockDataPayload(BlockInfo blockInfo,
        BlockSummaryBase blockSummary,
        AccountInfosRetrieved accountInfos,
        RewardStatusBase rewardStatus,
        Func<Task<BakerPoolStatus[]>> readAllBakerPoolStatusesFunc,
        Func<Task<PoolStatusPassiveDelegation>> passiveDelegationPoolStatusFunc)
    {
        BlockInfo = blockInfo;
        BlockSummary = blockSummary;
        AccountInfos = accountInfos;
        RewardStatus = rewardStatus;
        _readAllBakerPoolStatusesFunc = readAllBakerPoolStatusesFunc;
        _passiveDelegationPoolStatusFunc = passiveDelegationPoolStatusFunc;
    }

    public BlockInfo BlockInfo { get; }
    public BlockSummaryBase BlockSummary { get; }
    public AccountInfosRetrieved AccountInfos { get; }
    public RewardStatusBase RewardStatus { get; }

    public async Task<BakerPoolStatus[]> ReadAllBakerPoolStatuses()
    {
        return await _readAllBakerPoolStatusesFunc();
    }

    public async Task<PoolStatusPassiveDelegation> ReadPassiveDelegationPoolStatus()
    {
        return await _passiveDelegationPoolStatusFunc();
    }
}

public record GenesisBlockDataPayload : BlockDataPayload
{
    public GenesisBlockDataPayload(BlockInfo blockInfo,
        BlockSummaryBase blockSummary,
        AccountInfosRetrieved accountInfos,
        RewardStatusBase rewardStatus,
        IdentityProviderInfo[] genesisIdentityProviders,
        Func<Task<BakerPoolStatus[]>> readAllBakerPoolStatusesFunc,
        Func<Task<PoolStatusPassiveDelegation>> passiveDelegationPoolStatusFunc) 
        : base(blockInfo, blockSummary, accountInfos, rewardStatus, readAllBakerPoolStatusesFunc, passiveDelegationPoolStatusFunc)
    {
        GenesisIdentityProviders = genesisIdentityProviders;
    }

    public IdentityProviderInfo[] GenesisIdentityProviders { get; init; }
}

public record AccountInfosRetrieved(
    AccountInfo[] CreatedAccounts,
    AccountInfo[] BakersWithNewPendingChanges);
