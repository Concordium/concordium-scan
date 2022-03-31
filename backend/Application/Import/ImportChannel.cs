using System.Threading.Channels;
using System.Threading.Tasks;
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

public record BlockDataPayload(
    BlockInfo BlockInfo, 
    BlockSummary BlockSummary, 
    AccountInfosRetrieved AccountInfos,
    RewardStatus RewardStatus);

public record GenesisBlockDataPayload(
    BlockInfo BlockInfo,
    BlockSummary BlockSummary,
    AccountInfosRetrieved AccountInfos,
    RewardStatus RewardStatus, 
    IdentityProviderInfo[] GenesisIdentityProviders) : BlockDataPayload(BlockInfo, BlockSummary, AccountInfos, RewardStatus);

public record AccountInfosRetrieved(
    AccountInfo[] CreatedAccounts,
    AccountInfo[] BakersRemoved);