using System.Threading.Channels;
using System.Threading.Tasks;
using Concordium.Sdk.Types;

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

public record InitialImportState(ulong? MaxBlockHeight, BlockHash? GenesisBlockHash);

public record BlockDataEnvelope(BlockDataPayload Payload, ConsensusInfo ConsensusInfo);

public record BlockDataPayload
{
    private readonly Func<Task<BakerPoolStatus[]>> _readAllBakerPoolStatusesFunc;
    private readonly Func<Task<PassiveDelegationStatus>> _passiveDelegationPoolStatusFunc;

    public BlockDataPayload(
        BlockInfo blockInfo,
        IList<BlockItemSummary> blockItemSummaries,
        IChainParameters chainParameters,
        IList<ISpecialEvent> specialEvents,
        AccountInfosRetrieved accountInfos,
        RewardOverviewBase rewardStatus,
        Func<Task<BakerPoolStatus[]>> readAllBakerPoolStatusesFunc,
        Func<Task<PassiveDelegationStatus>> passiveDelegationPoolStatusFunc)
    {
        BlockInfo = blockInfo;
        BlockItemSummaries = blockItemSummaries;
        ChainParameters = chainParameters;
        SpecialEvents = specialEvents;
        AccountInfos = accountInfos;
        RewardStatus = rewardStatus;
        _readAllBakerPoolStatusesFunc = readAllBakerPoolStatusesFunc;
        _passiveDelegationPoolStatusFunc = passiveDelegationPoolStatusFunc;
    }

    public BlockInfo BlockInfo { get; }
    public IList<BlockItemSummary> BlockItemSummaries { get; }
    public IChainParameters ChainParameters { get; }
    public IList<ISpecialEvent> SpecialEvents { get; }
    public AccountInfosRetrieved AccountInfos { get; }
    public RewardOverviewBase RewardStatus { get; }

    public async Task<BakerPoolStatus[]> ReadAllBakerPoolStatuses()
    {
        return await _readAllBakerPoolStatusesFunc();
    }

    public async Task<PassiveDelegationStatus> ReadPassiveDelegationPoolStatus()
    {
        return await _passiveDelegationPoolStatusFunc();
    }
}

public record GenesisBlockDataPayload : BlockDataPayload
{
    public GenesisBlockDataPayload(
        IList<IpInfo> genesisIdentityProviders,
        BlockInfo blockInfo,
        IList<BlockItemSummary> blockItemSummaries,
        IChainParameters chainParameters,
        IList<ISpecialEvent> specialEvents,
        AccountInfosRetrieved accountInfos,
        RewardOverviewBase rewardStatus,
        Func<Task<BakerPoolStatus[]>> readAllBakerPoolStatusesFunc,
        Func<Task<PassiveDelegationStatus>> passiveDelegationPoolStatusFunc) 
        : base(blockInfo, blockItemSummaries, chainParameters, specialEvents, accountInfos, rewardStatus, readAllBakerPoolStatusesFunc, passiveDelegationPoolStatusFunc)
    {
        GenesisIdentityProviders = genesisIdentityProviders;
    }

    public IList<IpInfo> GenesisIdentityProviders { get; }
}

public record AccountInfosRetrieved(
    AccountInfo[] CreatedAccounts,
    AccountInfo[] BakersWithNewPendingChanges);
