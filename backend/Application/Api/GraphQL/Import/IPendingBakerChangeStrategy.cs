using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Application.Api.GraphQL.Import;

public interface IPendingBakerChangeStrategy
{
    Task<PendingBakerChange> SetPendingChangeOnBaker(BakerRemoved bakerRemoved);
    Task<PendingBakerChange> SetPendingChangeOnBaker(BakerStakeDecreased stakeDecreased);
    bool MustApplyPendingChangesDue(DateTimeOffset? nextPendingBakerChangeTime);
    DateTimeOffset GetEffectiveTime();
}

public class PreProtocol4Strategy : IPendingBakerChangeStrategy
{
    private readonly AccountInfo[] _accountInfos;
    private readonly BakerWriter _writer;
    private readonly BlockInfo _blockInfo;

    public PreProtocol4Strategy(AccountInfo[] accountInfos, BlockInfo blockInfo, BakerWriter writer)
    {
        _blockInfo = blockInfo;
        _writer = writer;
        _accountInfos = accountInfos;
    }

    public Task<PendingBakerChange> SetPendingChangeOnBaker(BakerRemoved bakerRemoved)
    {
        return SetPendingChangeOnBaker(bakerRemoved.Account);
    }

    public Task<PendingBakerChange> SetPendingChangeOnBaker(BakerStakeDecreased stakeDecreased)
    {
        return SetPendingChangeOnBaker(stakeDecreased.Account);
    }

    public bool MustApplyPendingChangesDue(DateTimeOffset? nextPendingBakerChangeTime)
    {
        return _blockInfo.BlockSlotTime >= nextPendingBakerChangeTime;
    }

    public DateTimeOffset GetEffectiveTime()
    {
        return _blockInfo.BlockSlotTime;
    }

    private async Task<PendingBakerChange> SetPendingChangeOnBaker(AccountAddress bakerAccountAddress)
    {
        var accountBaker = _accountInfos
            .SingleOrDefault(x => x.AccountAddress == bakerAccountAddress)?
            .AccountBaker ?? throw new InvalidOperationException("AccountInfo not included for baker -OR- was not a baker!");

        var updatedBaker = await _writer.UpdateBaker(accountBaker, src => src.BakerId,
            (src, dst) => SetPendingChange(dst, src, _blockInfo));

        var result = ((ActiveBakerState)updatedBaker.State).PendingChange!;
        return result;
    }

    private void SetPendingChange(Baker destination, AccountBaker source, BlockInfo blockInfo)
    {
        if (source.PendingChange == null) throw new ArgumentException("Pending change must not be null");

        var activeState = destination.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set a pending change for a baker that is not active!");
        activeState.PendingChange = source.PendingChange switch
        {
            AccountBakerRemovePendingV1 x => new PendingBakerRemoval(x.EffectiveTime),
            AccountBakerReduceStakePendingV1 x => new PendingBakerReduceStake(x.EffectiveTime, x.NewStake.MicroCcdValue),
            _ => throw new NotImplementedException($"Mapping not implemented for '{source.PendingChange.GetType().Name}'")
        };
    }
}

public class PostProtocol4Strategy : IPendingBakerChangeStrategy
{
    private readonly BlockInfo _blockInfo;
    private readonly ChainParametersV1 _chainParameters;
    private readonly BakerWriter _writer;
    private readonly BlockImportPaydayStatus _importPaydayStatus;

    public PostProtocol4Strategy(BlockInfo blockInfo, ChainParametersV1 chainParameters, BlockImportPaydayStatus importPaydayStatus, BakerWriter writer)
    {
        _blockInfo = blockInfo;
        _chainParameters = chainParameters;
        _importPaydayStatus = importPaydayStatus;
        _writer = writer;
    }

    public async Task<PendingBakerChange> SetPendingChangeOnBaker(BakerRemoved bakerRemoved)
    {
        var updatedBaker = await _writer.UpdateBaker(bakerRemoved, src => src.BakerId,
            (src, dst) => SetPendingChange(dst, src));
        if (updatedBaker is null)
        {
            return null;
        }
        
        return ((ActiveBakerState)updatedBaker.State).PendingChange!;
    }

    public async Task<PendingBakerChange> SetPendingChangeOnBaker(BakerStakeDecreased stakeDecreased)
    {
        var updatedBaker = await _writer.UpdateBaker(stakeDecreased, src => src.BakerId,
            (src, dst) => SetPendingChange(dst, src));

        if (updatedBaker is null)
        {
            return null;
        }

        return ((ActiveBakerState)updatedBaker.State).PendingChange!;
    }

    public bool MustApplyPendingChangesDue(DateTimeOffset? nextPendingBakerChangeTime)
    {
        if (_importPaydayStatus is FirstBlockAfterPayday firstBlockAfterPayday)
            return firstBlockAfterPayday.PaydayTimestamp >= nextPendingBakerChangeTime;
        return false;
    }

    public DateTimeOffset GetEffectiveTime()
    {
        if (_importPaydayStatus is FirstBlockAfterPayday firstBlockAfterPayday)
            return firstBlockAfterPayday.PaydayTimestamp;
        throw new InvalidOperationException("This method should only be called if pending changes must be applied.");
    }

    private void SetPendingChange(Baker destination, object source)
    {
        var activeState = destination.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set a pending change for a baker that is not active!");
        var effectiveTime = _blockInfo.BlockSlotTime.AddSeconds(_chainParameters.PoolOwnerCooldown);

        activeState.PendingChange = source switch
        {
            BakerRemoved => new PendingBakerRemoval(effectiveTime),
            BakerStakeDecreased x => new PendingBakerReduceStake(effectiveTime, x.NewStake.MicroCcdValue),
            _ => throw new NotImplementedException($"Mapping not implemented for '{source.GetType().Name}'")
        };
    }
}