using System.Threading.Channels;
using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Application.Import;

public class ImportChannel
{
    private readonly Channel<BlockDataEnvelope> _channel;
    private readonly TaskCompletionSource<ImportState> _importStateTaskCompletionSource;

    public ImportChannel()
    {
        var options = new BoundedChannelOptions(30)
        {
            FullMode = BoundedChannelFullMode.Wait
        };
        _channel = Channel.CreateBounded<BlockDataEnvelope>(options);
        _importStateTaskCompletionSource = new TaskCompletionSource<ImportState>();
    }

    public ChannelWriter<BlockDataEnvelope> Writer => _channel.Writer;
    public ChannelReader<BlockDataEnvelope> Reader => _channel.Reader;

    public void SetInitialImportState(ImportState importState)
    {
        _importStateTaskCompletionSource.SetResult(importState);
    }

    public Task<ImportState> GetInitialImportStateAsync()
    {
        return _importStateTaskCompletionSource.Task;
    }
}

public record ImportState(long? MaxBlockHeight, BlockHash? GenesisBlockHash);

public record BlockDataEnvelope(BlockDataPayload Payload, TimeSpan ReadDuration);

public record BlockDataPayload(
    BlockInfo BlockInfo, 
    BlockSummary BlockSummary, 
    AccountInfo[] CreatedAccounts,
    RewardStatus RewardStatus);

public record GenesisBlockDataPayload(
    BlockInfo BlockInfo,
    BlockSummary BlockSummary,
    AccountInfo[] CreatedAccounts,
    RewardStatus RewardStatus, 
    IdentityProviderInfo[] GenesisIdentityProviders) : BlockDataPayload(BlockInfo, BlockSummary, CreatedAccounts, RewardStatus);

