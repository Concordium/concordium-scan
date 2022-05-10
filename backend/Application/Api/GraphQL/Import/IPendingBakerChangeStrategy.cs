using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Application.Api.GraphQL.Import;

public interface IPendingBakerChangeStrategy
{
    Task<PendingBakerChange> SetPendingChangeOnBaker(BakerRemoved bakerRemoved);
    Task<PendingBakerChange> SetPendingChangeOnBaker(BakerStakeDecreased stakeDecreased);
    bool MustApplyPendingChangesDue();
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

    public bool MustApplyPendingChangesDue()
    {
        return true;
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
            AccountBakerRemovePendingV0 x => new PendingBakerRemoval(CalculateEffectiveTime(x.Epoch, blockInfo.BlockSlotTime, blockInfo.BlockSlot), x.Epoch), 
            AccountBakerReduceStakePendingV0 x => new PendingBakerReduceStake(CalculateEffectiveTime(x.Epoch, blockInfo.BlockSlotTime, blockInfo.BlockSlot), x.NewStake.MicroCcdValue, x.Epoch),
            _ => throw new NotImplementedException($"Mapping not implemented for '{source.PendingChange.GetType().Name}'")
        };
    }

    public static DateTimeOffset CalculateEffectiveTime(ulong epoch, DateTimeOffset blockSlotTime, int blockSlot)
    {
        // TODO: Prior to protocol update 4, the effective time must be calculated in this cumbersome way
        //       We should be able to change this once we switch to concordium node v4 or greater!
        //
        // BUILT-IN ASSUMPTIONS (that can change but probably wont):
        //       Block time is 250ms
        //       Epoch duration is 1 hour
        
        var millisecondsSinceEraGenesis = (long)blockSlot * 250; // cast to long to avoid overflow!
        var eraGenesisTime = blockSlotTime.AddMilliseconds(-1 * millisecondsSinceEraGenesis);
        var effectiveTime = eraGenesisTime.AddHours(epoch);
        
        return effectiveTime;
    }
}

public class PostProtocol4Strategy : IPendingBakerChangeStrategy
{
    private readonly BlockInfo _blockInfo;
    private readonly ChainParametersV1 _chainParameters;
    private readonly BakerWriter _writer;
    private readonly bool _isFirstBlockAfterPayday;

    public PostProtocol4Strategy(BlockInfo blockInfo, ChainParametersV1 chainParameters, bool isFirstBlockAfterPayday, BakerWriter writer)
    {
        _blockInfo = blockInfo;
        _chainParameters = chainParameters;
        _isFirstBlockAfterPayday = isFirstBlockAfterPayday;
        _writer = writer;
    }

    public async Task<PendingBakerChange> SetPendingChangeOnBaker(BakerRemoved bakerRemoved)
    {
        var updatedBaker = await _writer.UpdateBaker(bakerRemoved, src => src.BakerId,
            (src, dst) => SetPendingChange(dst, src));
        return ((ActiveBakerState)updatedBaker.State).PendingChange!;
    }

    public async Task<PendingBakerChange> SetPendingChangeOnBaker(BakerStakeDecreased stakeDecreased)
    {
        var updatedBaker = await _writer.UpdateBaker(stakeDecreased, src => src.BakerId,
            (src, dst) => SetPendingChange(dst, src));
        return ((ActiveBakerState)updatedBaker.State).PendingChange!;
    }

    public bool MustApplyPendingChangesDue()
    {
        return _isFirstBlockAfterPayday;
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