using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Import;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import;

internal interface IBakerChangeStrategy
{
    Task UpdateBakersFromTransactionEvents(
        IEnumerable<AccountTransactionDetails> transactionEvents,
        ImportState importState,
        BakerImportHandler.BakerUpdateResultsBuilder resultBuilder);
    
    bool MustApplyPendingChangesDue(DateTimeOffset? nextPendingBakerChangeTime);
    DateTimeOffset GetEffectiveTime();
}

internal static class BakerChangeStrategyFactory
{
    internal static IBakerChangeStrategy Create(
        BlockInfo blockInfo,
        ChainParameters chainParameters, 
        BlockImportPaydayStatus importPaydayStatus,
        BakerWriter writer,
        AccountInfo[] bakersWithNewPendingChanges)
    {
        if (blockInfo.ProtocolVersion.AsInt() < 4)
            return new PreProtocol4Strategy(bakersWithNewPendingChanges, blockInfo, writer);
        
        ChainParameters.TryGetPoolOwnerCooldown(chainParameters, out var poolOwnerCooldown);
        return new PostProtocol4Strategy(blockInfo, poolOwnerCooldown!.Value, importPaydayStatus, writer);

    }
}

public class PreProtocol4Strategy : IBakerChangeStrategy
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

    public async Task UpdateBakersFromTransactionEvents(
        IEnumerable<AccountTransactionDetails> transactionEvents,
        ImportState importState,
        BakerImportHandler.BakerUpdateResultsBuilder resultBuilder)
    {
        foreach (var txEvent in transactionEvents)
        {
            switch (txEvent.Effects)
            {
                case BakerAdded bakerAdded:
                    var pool = importState.MigrationToBakerPoolsCompleted ? BakerPool.CreateDefaultBakerPool() : null;
                
                    await _writer.AddOrUpdateBaker(bakerAdded,
                        src => src.KeysEvent.BakerId.Id.Index,
                        src => Baker.CreateNewBaker(src.KeysEvent.BakerId, src.Stake, src.RestakeEarnings, pool),
                        (src, dst) =>
                        {
                            dst.State = new ActiveBakerState(src.Stake.Value, src.RestakeEarnings, pool, null);
                        });

                    resultBuilder.IncrementBakersAdded();
                    break;
                case BakerRemoved:
                    var pendingChange = await SetPendingChangeOnBaker(txEvent.Sender);
                    importState.UpdateNextPendingBakerChangeTimeIfLower(pendingChange.EffectiveTime);    
                    break;
                case BakerStakeUpdated bakerStakeUpdated:
                    if (bakerStakeUpdated.Data is null)
                    {
                        continue;
                    };
                    switch (bakerStakeUpdated.Data!.Increased)
                    {
                        case true:
                            await _writer.UpdateBaker(bakerStakeUpdated.Data!,
                                src => src.BakerId.Id.Index,
                                (src, dst) =>
                                {
                                    var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                                    activeState.StakedAmount = src.NewStake.Value;
                                });
                            break;
                        case false:
                            var pendingChangeUpdate = await SetPendingChangeOnBaker(txEvent.Sender);
                            importState.UpdateNextPendingBakerChangeTimeIfLower(pendingChangeUpdate.EffectiveTime);
                            break;
                    }
                    await _writer.UpdateBaker(bakerStakeUpdated.Data!,
                        src => src.BakerId.Id.Index,
                        (src, dst) =>
                        {
                            var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                            activeState.StakedAmount = src.NewStake.Value; 
                        });
                    break;

                case BakerRestakeEarningsUpdated bakerRestakeEarningsUpdated:
                    await _writer.UpdateBaker(bakerRestakeEarningsUpdated,
                        src => src.BakerId.Id.Index,
                        (src, dst) =>
                        {
                            var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                            activeState.RestakeEarnings = src.RestakeEarnings;
                        });
                    break;
                case BakerConfigured:
                case BakerKeysUpdated: 
                default:
                    break;
            }
        }
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
        var singleAccountInfo = _accountInfos
            .SingleOrDefault(x => x.AccountAddress == bakerAccountAddress);
        if (singleAccountInfo?.AccountStakingInfo is not AccountBaker accountBaker)
        {
            throw new InvalidOperationException("AccountInfo not included for baker -OR- was not a baker!");
        }

        var updatedBaker = await _writer.UpdateBaker(accountBaker, src => src.BakerInfo.BakerId.Id.Index,
            (src, dst) => SetPendingChange(dst, src));

        var result = ((ActiveBakerState)updatedBaker.State).PendingChange!;
        return result;
    }

    private static void SetPendingChange(Baker destination, AccountBaker source)
    {
        if (source.PendingChange == null) throw new ArgumentException("Pending change must not be null");
        
        var activeState = destination.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set a pending change for a baker that is not active!");
        activeState.PendingChange = source.PendingChange switch
        {
            AccountBakerRemovePending x => new PendingBakerRemoval(x.EffectiveTime), 
            AccountBakerReduceStakePending x => new PendingBakerReduceStake(x.EffectiveTime, x.NewStake.Value),
            _ => throw new NotImplementedException($"Mapping not implemented for '{source.PendingChange.GetType().Name}'")
        };
    }
}

public class PostProtocol4Strategy : IBakerChangeStrategy
{
    private readonly BlockInfo _blockInfo;
    private readonly ulong _poolOwnerCooldown;
    private readonly BakerWriter _writer;
    private readonly BlockImportPaydayStatus _importPaydayStatus;

    public PostProtocol4Strategy(BlockInfo blockInfo, ulong poolOwnerCooldown, BlockImportPaydayStatus importPaydayStatus, BakerWriter writer)
    {
        _blockInfo = blockInfo;
        _poolOwnerCooldown = poolOwnerCooldown;
        _importPaydayStatus = importPaydayStatus;
        _writer = writer;
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

    public async Task UpdateBakersFromTransactionEvents(IEnumerable<AccountTransactionDetails> transactionEvents, ImportState importState,
        BakerImportHandler.BakerUpdateResultsBuilder resultBuilder)
    {
        foreach (var txEvent in transactionEvents)
        {
            switch (txEvent.Effects)
            {
                case BakerConfigured bakerConfigured:
                    foreach (var bakerEvent in bakerConfigured.Data)
                    {
                        switch (bakerEvent)
                        {
                            case BakerAddedEvent bakerAdded:
                                var pool = importState.MigrationToBakerPoolsCompleted ? BakerPool.CreateDefaultBakerPool() : null;
                                await _writer.AddOrUpdateBaker(bakerAdded,
                                    src => src.KeysEvent.BakerId.Id.Index,
                                    src => Baker.CreateNewBaker(src.KeysEvent.BakerId, src.Stake, src.RestakeEarnings, pool),
                                    (src, dst) =>
                                    {
                                        dst.State = new ActiveBakerState(src.Stake.Value, src.RestakeEarnings, pool, null);
                                    });

                                resultBuilder.IncrementBakersAdded();
                                break;
                            case BakerRemovedEvent bakerRemovedEvent:
                                var pendingChange = await SetPendingChangeOnBaker(bakerRemovedEvent.BakerId, bakerRemovedEvent);
                                importState.UpdateNextPendingBakerChangeTimeIfLower(pendingChange.EffectiveTime);
                                break;
                            case BakerRestakeEarningsUpdatedEvent bakerRestakeEarningsUpdatedEvent:
                                await _writer.UpdateBaker(bakerRestakeEarningsUpdatedEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                                        activeState.RestakeEarnings = src.RestakeEarnings;
                                    });
                                break;
                            case BakerSetTransactionFeeCommissionEvent bakerSetTransactionFeeCommissionEvent:
                                await _writer.UpdateBaker(bakerSetTransactionFeeCommissionEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var transactionBakerPool = dst.GetPool();
                                        transactionBakerPool.CommissionRates.TransactionCommission = src.TransactionFeeCommission.AsDecimal();
                                    });
                                break;                            
                            case BakerSetBakingRewardCommissionEvent bakerSetBakingRewardCommissionEvent:
                                await _writer.UpdateBaker(bakerSetBakingRewardCommissionEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var bakingRewardBakerPool = dst.GetPool();
                                        bakingRewardBakerPool.CommissionRates.BakingCommission = src.BakingRewardCommission.AsDecimal();
                                    });
                                break;
                            case BakerSetFinalizationRewardCommissionEvent bakerSetFinalizationRewardCommissionEvent:
                                await _writer.UpdateBaker(bakerSetFinalizationRewardCommissionEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var finalizationRewardBakerPool = dst.GetPool();
                                        finalizationRewardBakerPool.CommissionRates.FinalizationCommission = src.FinalizationRewardCommission.AsDecimal();
                                    });
                                break;
                            case BakerSetMetadataUrlEvent bakerSetMetadataUrlEvent:
                                await _writer.UpdateBaker(bakerSetMetadataUrlEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var metaBakerPool = dst.GetPool();
                                        metaBakerPool.MetadataUrl = src.MetadataUrl;
                                    });
                                break;
                            case BakerSetOpenStatusEvent bakerSetOpenStatusEvent:
                                await _writer.UpdateBaker(bakerSetOpenStatusEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var openStatusBakerPool = dst.GetPool();
                                        openStatusBakerPool.OpenStatus = src.OpenStatus.MapToGraphQlEnum();
                                    });

                                if (bakerSetOpenStatusEvent.OpenStatus == Concordium.Sdk.Types.BakerPoolOpenStatus.ClosedForAll)
                                    resultBuilder.AddBakerClosedForAll((long)bakerSetOpenStatusEvent.BakerId.Id.Index);
                                break;
                            case BakerStakeDecreasedEvent bakerStakeDecreasedEvent:
                                var pendingChangeStakeDecreased = await SetPendingChangeOnBaker(bakerStakeDecreasedEvent.BakerId, bakerStakeDecreasedEvent);
                                importState.UpdateNextPendingBakerChangeTimeIfLower(pendingChangeStakeDecreased.EffectiveTime);
                                break;
                            case BakerStakeIncreasedEvent bakerStakeIncreasedEvent:
                                await _writer.UpdateBaker(bakerStakeIncreasedEvent,
                                    src => src.BakerId.Id.Index,
                                    (src, dst) =>
                                    {
                                        var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                                        activeState.StakedAmount = src.NewStake.Value;
                                    });
                                break;
                            case BakerKeysUpdatedEvent:
                            default:
                                break;
                        }
                    }
                    break;
                default:
                    break;
            }
        }
    }
    
    private async Task<PendingBakerChange> SetPendingChangeOnBaker<T>(BakerId stakeDecreased, T pendingChangeType) where T : IBakerEvent
    {
        var updatedBaker = await _writer.UpdateBaker(stakeDecreased, src => src.Id.Index,
            (_, dst) => SetPendingChange(dst, pendingChangeType));
        return ((ActiveBakerState)updatedBaker.State).PendingChange!;
    }
    
    private void SetPendingChange<T>(Baker destination, T pendingChangeType) where T : IBakerEvent
    {
        var activeState = destination.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set a pending change for a baker that is not active!");
        var effectiveTime = _blockInfo.BlockSlotTime.AddSeconds(_poolOwnerCooldown);

        activeState.PendingChange = pendingChangeType switch
        {
            BakerRemovedEvent => new PendingBakerRemoval(effectiveTime), 
            BakerStakeDecreasedEvent stakeDecreasedEvent => new PendingBakerReduceStake(effectiveTime, stakeDecreasedEvent.NewStake.Value),
            _ => throw new NotImplementedException($"Mapping not implemented for '{pendingChangeType.GetType().Name}'")
        };
    }
}