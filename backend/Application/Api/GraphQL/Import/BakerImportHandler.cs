using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Application.Import;
using ConcordiumSdk.NodeApi;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Microsoft.EntityFrameworkCore;
using BakerPoolOpenStatus = Application.Api.GraphQL.Bakers.BakerPoolOpenStatus;
using CommissionRates = Application.Api.GraphQL.Bakers.CommissionRates;

namespace Application.Api.GraphQL.Import;

public class BakerImportHandler
{
    private readonly BakerWriter _writer;
    private readonly IMetrics _metrics;
    private readonly ILogger _logger;

    public BakerImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IMetrics metrics)
    {
        _writer = new BakerWriter(dbContextFactory, metrics);
        _metrics = metrics;
        _logger = Log.ForContext(GetType());
    }

    public async Task<BakerUpdateResults> HandleBakerUpdates(BlockDataPayload payload, RewardsSummary rewardsSummary,
        ChainParametersState chainParameters, BlockImportPaydayStatus importPaydayStatus, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerImportHandler), nameof(HandleBakerUpdates));

        IPendingBakerChangeStrategy pendingChangeStrategy = payload.BlockSummary.ProtocolVersion >= 4
            ? new PostProtocol4Strategy(payload.BlockInfo, (ChainParametersV1)chainParameters.Current, importPaydayStatus, _writer)
            : new PreProtocol4Strategy(payload.AccountInfos.BakersWithNewPendingChanges, payload.BlockInfo, _writer);

        var resultBuilder = new BakerUpdateResultsBuilder();

        if (importPaydayStatus is FirstBlockAfterPayday)
        {
            var stakeSnapshot = await _writer.GetPaydayPoolStakeSnapshot();
            resultBuilder.SetPaydayStakeSnapshot(stakeSnapshot);
        }

        if (payload is GenesisBlockDataPayload)
            await AddGenesisBakers(payload, resultBuilder, importState);
        else
            await ApplyBakerChanges(payload, rewardsSummary, chainParameters, pendingChangeStrategy, importState, resultBuilder, importPaydayStatus is FirstBlockAfterPayday);

        var totalAmountStaked = await _writer.GetTotalAmountStaked();
        resultBuilder.SetTotalAmountStaked(totalAmountStaked);
        return resultBuilder.Build();
    }

    public async Task ApplyChangesAfterBlocksAndTransactionsWritten(Block block, TransactionPair[] transactions,
        BlockImportPaydayStatus importPaydayStatus)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerImportHandler), nameof(ApplyChangesAfterBlocksAndTransactionsWritten));

        if (importPaydayStatus is FirstBlockAfterPayday)
            await _writer.UpdateTemporaryBakerPoolPaydayStatusesWithPayoutBlockId(block.Id);

        var items = transactions
            .Select(tx =>
            {
                var bakerIds = tx.Source.Result.GetBakerIds().Distinct().ToArray();
                if (bakerIds.Length == 0)
                    return null;
                if (bakerIds.Length == 1)
                    return new BakerTransactionRelation
                    {
                        BakerId = (long)bakerIds.Single(),
                        TransactionId = tx.Target.Id
                    };
                throw new InvalidOperationException("Did not expect multiple baker id's from one transaction");
            })
            .Where(x => x != null)
            .Select(x => x!);

        await _writer.AddBakerTransactionRelations(items);
    }

    public async Task ApplyDelegationUpdates(BlockDataPayload payload, DelegationUpdateResults delegationUpdateResults,
        BakerUpdateResults bakerUpdateResults, ChainParameters chainParameters)
    {
        if (payload.BlockSummary.ProtocolVersion >= 4) // TODO: Could be optimized by only invoking on payday block (?)
            await _writer.UpdateDelegatedStake();

        foreach (var delegatorCountDelta in delegationUpdateResults.DelegatorCountDeltas)
        {
            // Could be optimized by using a raw sql statement, but few delegation target changes are expected per block.
            if (delegatorCountDelta.DelegationTarget is BakerDelegationTarget bakerTarget)
                await _writer.UpdateBaker(bakerTarget, obj => (ulong)obj.BakerId, (_, dst) =>
                {
                    if (dst.State is ActiveBakerState activeState) // Check, since baker might have been removed in this block!
                        activeState.Pool!.DelegatorCount += delegatorCountDelta.DelegatorCountDelta;
                });
        }

        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            var chainParametersV1 = (ChainParametersV1)chainParameters;
            var capitalBound = chainParametersV1.CapitalBound;
            var leverageFactor = chainParametersV1.LeverageBound.AsDecimal();

            var totalAmountStaked = bakerUpdateResults.TotalAmountStaked + delegationUpdateResults.TotalAmountStaked;
            await _writer.UpdateDelegatedStakeCap(totalAmountStaked, capitalBound, leverageFactor);
        }
    }

    private async Task AddGenesisBakers(BlockDataPayload payload, BakerUpdateResultsBuilder resultBuilder, ImportState importState)
    {
        var mapBakerPool = payload.BlockSummary.ProtocolVersion >= 4;

        var genesisBakers = payload.AccountInfos.CreatedAccounts
            .Where(x => x.AccountBaker != null)
            .Select(x => x.AccountBaker!)
            .Select(x => CreateGenesisBaker(x, mapBakerPool))
            .ToArray();

        await _writer.AddBakers(genesisBakers);

        if (mapBakerPool)
            importState.MigrationToBakerPoolsCompleted = true;

        resultBuilder.IncrementBakersAdded(genesisBakers.Length);
    }

    private static Baker CreateGenesisBaker(AccountBaker src, bool mapBakerPool)
    {
        var pool = mapBakerPool ? MapBakerPool(src) : null;

        return CreateNewBaker(src.BakerId, src.StakedAmount, src.RestakeEarnings, pool);
    }

    private static BakerPool MapBakerPool(AccountBaker accountBaker)
    {
        var poolInfo = accountBaker.BakerPoolInfo;
        if (poolInfo == null) throw new ArgumentNullException(nameof(accountBaker), "Did not expect baker pool info of the account to be null when trying to map it!");

        return new BakerPool
        {
            OpenStatus = poolInfo.OpenStatus.MapToGraphQlEnum(),
            MetadataUrl = poolInfo.MetadataUrl,
            CommissionRates = new CommissionRates
            {
                TransactionCommission = poolInfo.CommissionRates.TransactionCommission,
                FinalizationCommission = poolInfo.CommissionRates.FinalizationCommission,
                BakingCommission = poolInfo.CommissionRates.BakingCommission
            },
            PaydayStatus = new CurrentPaydayStatus()
            {
                BakerStake = accountBaker.StakedAmount.MicroCcdValue,
                DelegatedStake = 0
            }
        };
    }

    private async Task ApplyBakerChanges(BlockDataPayload payload, RewardsSummary rewardsSummary,
        ChainParametersState chainParameters, IPendingBakerChangeStrategy pendingChangeStrategy,
        ImportState importState, BakerUpdateResultsBuilder resultBuilder, bool isFirstBlockAfterPayday)
    {
        await MaybeMigrateToBakerPools(payload, importState);
        await MaybeApplyCommissionRangeChanges(chainParameters);
        await WorkAroundConcordiumNodeBug225(payload.BlockInfo, importState);

        await UpdateBakersWithPendingChangesDue(pendingChangeStrategy, importState, resultBuilder);

        var allTransactionEvents = payload.BlockSummary.TransactionSummaries
            .Select(tx => tx.Result).OfType<TransactionSuccessResult>()
            .SelectMany(x => x.Events)
            .ToArray();

        var txEvents = allTransactionEvents.Where(x => x
            is ConcordiumSdk.NodeApi.Types.BakerAdded
            or ConcordiumSdk.NodeApi.Types.BakerRemoved
            or ConcordiumSdk.NodeApi.Types.BakerStakeIncreased
            or ConcordiumSdk.NodeApi.Types.BakerStakeDecreased
            or ConcordiumSdk.NodeApi.Types.BakerSetRestakeEarnings
            or ConcordiumSdk.NodeApi.Types.BakerSetOpenStatus
            or ConcordiumSdk.NodeApi.Types.BakerSetMetadataURL
            or ConcordiumSdk.NodeApi.Types.BakerSetTransactionFeeCommission
            or ConcordiumSdk.NodeApi.Types.BakerSetFinalizationRewardCommission
            or ConcordiumSdk.NodeApi.Types.BakerSetBakingRewardCommission);

        await UpdateBakersFromTransactionEvents(txEvents, pendingChangeStrategy, importState, resultBuilder);

        // This should happen after the bakers from current block has been added to the database
        if (isFirstBlockAfterPayday)
        {
            await UpdateCurrentPaydayStatusOnAllBakers(payload);
        }

        await _writer.UpdateStakeIfBakerActiveRestakingEarnings(rewardsSummary.AggregatedAccountRewards);
    }

    private async Task UpdateCurrentPaydayStatusOnAllBakers(BlockDataPayload payload)
    {
        await _writer.CreateTemporaryBakerPoolPaydayStatuses();

        var poolStatuses = await payload.ReadAllBakerPoolStatuses();
        foreach (var poolStatus in poolStatuses)
        {
            await _writer.UpdateBaker(poolStatus, src => src.BakerId, (src, dst) =>
            {
                var pool = dst.ActiveState!.Pool ?? throw new InvalidOperationException("Did not expect this bakers pool property to be null");

                var status = src.CurrentPaydayStatus;
                ApplyPaydayStatus(pool, status);
            });
        }
    }

    private static void ApplyPaydayStatus(BakerPool pool, CurrentPaydayBakerPoolStatus? source)
    {
        if (source != null)
        {
            if (pool.PaydayStatus == null)
                pool.PaydayStatus = new CurrentPaydayStatus();
            pool.PaydayStatus.BakerStake = source.BakerEquityCapital.MicroCcdValue;
            pool.PaydayStatus.DelegatedStake = source.DelegatedCapital.MicroCcdValue;
            pool.PaydayStatus.EffectiveStake = source.EffectiveStake.MicroCcdValue;
            pool.PaydayStatus.LotteryPower = source.LotteryPower;
        }
        else
            pool.PaydayStatus = null;
    }

    private async Task MaybeMigrateToBakerPools(BlockDataPayload payload, ImportState importState)
    {
        // Migrate to baker pool first time a block with protocol version 4 (or greater) is encountered.
        if (importState.MigrationToBakerPoolsCompleted || !payload.BlockSummary.ProtocolVersion.HasValue || payload.BlockSummary.ProtocolVersion.Value < 4)
            return;

        _logger.Information("Migrating all bakers to baker pools (protocol v4 update)...");

        var bakerPoolStatuses = await payload.ReadAllBakerPoolStatuses();
        var bakerPoolStatusesDict = bakerPoolStatuses
            .ToDictionary(x => (long)x.BakerId);

        await _writer.UpdateBakers(
            baker =>
            {
                var source = bakerPoolStatusesDict[baker.BakerId];

                var pool = new BakerPool
                {
                    OpenStatus = source.PoolInfo.OpenStatus.MapToGraphQlEnum(),
                    MetadataUrl = source.PoolInfo.MetadataUrl,
                    CommissionRates = new CommissionRates
                    {
                        TransactionCommission = source.PoolInfo.CommissionRates.TransactionCommission,
                        FinalizationCommission = source.PoolInfo.CommissionRates.FinalizationCommission,
                        BakingCommission = source.PoolInfo.CommissionRates.BakingCommission
                    },
                    DelegatedStake = source.DelegatedCapital.MicroCcdValue,
                    DelegatorCount = 0,
                    DelegatedStakeCap = source.DelegatedCapitalCap.MicroCcdValue,
                    TotalStake = source.BakerEquityCapital.MicroCcdValue + source.DelegatedCapital.MicroCcdValue
                };
                ApplyPaydayStatus(pool, source.CurrentPaydayStatus);

                baker.ActiveState!.Pool = pool;
            },
            baker => baker.ActiveState != null);

        importState.MigrationToBakerPoolsCompleted = true;
        _logger.Information("Migration completed!");
    }

    private async Task MaybeApplyCommissionRangeChanges(ChainParametersState chainParameters)
    {
        if (chainParameters is ChainParametersChangedState { Current: ChainParametersV1 current, Previous: ChainParametersV1 previous })
        {
            if (current.FinalizationCommissionRange.Equals(previous.FinalizationCommissionRange)
                && current.BakingCommissionRange.Equals(previous.BakingCommissionRange)
                && current.TransactionCommissionRange.Equals(previous.TransactionCommissionRange))
                return; // No commission ranges changed!

            _logger.Information("Applying commission range changes to baker pools");

            await _writer.UpdateBakers(baker =>
                {
                    var rates = baker.ActiveState!.Pool!.CommissionRates;
                    rates.FinalizationCommission = AdjustValueToRange(rates.FinalizationCommission, current.FinalizationCommissionRange);
                    rates.BakingCommission = AdjustValueToRange(rates.BakingCommission, current.BakingCommissionRange);
                    rates.TransactionCommission = AdjustValueToRange(rates.TransactionCommission, current.TransactionCommissionRange);
                },
                baker => baker.ActiveState!.Pool != null);

            _logger.Information("Commission range changed applied!");
        }
    }

    private decimal AdjustValueToRange(decimal currentValue, CommissionRange allowedRange)
    {
        if (currentValue < allowedRange.Min)
            return allowedRange.Min;
        if (currentValue > allowedRange.Max)
            return allowedRange.Max;
        return currentValue;
    }

    private async Task WorkAroundConcordiumNodeBug225(BlockInfo blockInfo, ImportState importState)
    {
        if (blockInfo.GenesisIndex > importState.LastGenesisIndex)
        {
            // Work-around for bug in concordium node: https://github.com/Concordium/concordium-node/issues/225
            // A pending change will be (significantly) prolonged if a change to a baker is pending when a
            // protocol update occurs (causing a new era to start and thus resetting epoch to zero)
            // Only baker 1900 on mainnet is affected by this bug (after the testnet reset in June 2022).

            importState.LastGenesisIndex = blockInfo.GenesisIndex;

            var networkId = ConcordiumNetworkId.TryGetFromGenesisBlockHash(new BlockHash(importState.GenesisBlockHash));
            var isMainnet = networkId == ConcordiumNetworkId.Mainnet;
            if (isMainnet && blockInfo.GenesisIndex == 2 && blockInfo.BlockHeight == 1848787)
            {
                _logger.Information("Genesis index 2 detected on mainnet at expected block height. Will update effective time on baker 1900.");

                await _writer.UpdateBaker(1900UL, bakerId => bakerId, (bakerId, baker) =>
                {
                    var activeState = (ActiveBakerState)baker.State;
                    activeState.PendingChange = new PendingBakerRemoval(new DateTimeOffset(2022, 04, 11, 20, 0, 3, 750, TimeSpan.Zero));
                });

                importState.NextPendingBakerChangeTime = await _writer.GetMinPendingChangeTime();
                _logger.Information("NextPendingBakerChangeTime set to {value}", importState.NextPendingBakerChangeTime);
            }
        }
    }

    private async Task UpdateBakersFromTransactionEvents(IEnumerable<TransactionResultEvent> transactionEvents,
        IPendingBakerChangeStrategy pendingChangeStrategy, ImportState importState,
        BakerUpdateResultsBuilder resultBuilder)
    {
        foreach (var txEvent in transactionEvents)
        {
            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerAdded bakerAdded)
            {
                var pool = importState.MigrationToBakerPoolsCompleted ? CreateDefaultBakerPool() : null;

                await _writer.AddOrUpdateBaker(bakerAdded,
                    src => src.BakerId,
                    src => CreateNewBaker(src.BakerId, src.Stake, src.RestakeEarnings, pool),
                    (src, dst) =>
                    {
                        dst.State = new ActiveBakerState(src.Stake.MicroCcdValue, src.RestakeEarnings, pool, null);
                    });

                resultBuilder.IncrementBakersAdded();
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerRemoved bakerRemoved)
            {
                var pendingChange = await pendingChangeStrategy.SetPendingChangeOnBaker(bakerRemoved);
                UpdateNextPendingBakerChangeTimeIfLower(pendingChange.EffectiveTime, importState);
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerStakeDecreased stakeDecreased)
            {
                var pendingChange = await pendingChangeStrategy.SetPendingChangeOnBaker(stakeDecreased);
                UpdateNextPendingBakerChangeTimeIfLower(pendingChange.EffectiveTime, importState);
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerStakeIncreased stakeIncreased)
            {
                await _writer.UpdateBaker(stakeIncreased,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                        activeState.StakedAmount = src.NewStake.MicroCcdValue;
                    });
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetRestakeEarnings restakeEarnings)
            {
                await _writer.UpdateBaker(restakeEarnings,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set restake earnings for a baker that is not active!");
                        activeState.RestakeEarnings = src.RestakeEarnings;
                    });
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetOpenStatus openStatus)
            {
                await _writer.UpdateBaker(openStatus,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var pool = GetPool(dst);
                        pool.OpenStatus = src.OpenStatus.MapToGraphQlEnum();
                    });

                if (openStatus.OpenStatus == ConcordiumSdk.NodeApi.Types.BakerPoolOpenStatus.ClosedForAll)
                    resultBuilder.AddBakerClosedForAll((long)openStatus.BakerId);
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetMetadataURL metadataUrl)
            {
                await _writer.UpdateBaker(metadataUrl,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var pool = GetPool(dst);
                        pool.MetadataUrl = src.MetadataURL;
                    });
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetTransactionFeeCommission transactionFeeCommission)
            {
                await _writer.UpdateBaker(transactionFeeCommission,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var pool = GetPool(dst);
                        pool.CommissionRates.TransactionCommission = src.TransactionFeeCommission;
                    });
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetFinalizationRewardCommission finalizationRewardCommission)
            {
                await _writer.UpdateBaker(finalizationRewardCommission,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var pool = GetPool(dst);
                        pool.CommissionRates.FinalizationCommission = src.FinalizationRewardCommission;
                    });
            }

            if (txEvent is ConcordiumSdk.NodeApi.Types.BakerSetBakingRewardCommission bakingRewardCommission)
            {
                await _writer.UpdateBaker(bakingRewardCommission,
                    src => src.BakerId,
                    (src, dst) =>
                    {
                        var pool = GetPool(dst);
                        pool.CommissionRates.BakingCommission = src.BakingRewardCommission;
                    });
            }
        }
    }

    private void UpdateNextPendingBakerChangeTimeIfLower(DateTimeOffset pendingChangeTime, ImportState importState)
    {
        if (!importState.NextPendingBakerChangeTime.HasValue ||
            importState.NextPendingBakerChangeTime.Value > pendingChangeTime)
            importState.NextPendingBakerChangeTime = pendingChangeTime;
    }

    private async Task UpdateBakersWithPendingChangesDue(IPendingBakerChangeStrategy pendingChangeStrategy,
        ImportState importState, BakerUpdateResultsBuilder resultBuilder)
    {
        if (pendingChangeStrategy.MustApplyPendingChangesDue(importState.NextPendingBakerChangeTime))
        {
            var effectiveTime = pendingChangeStrategy.GetEffectiveTime();
            await _writer.UpdateBakersWithPendingChange(effectiveTime, baker => ApplyPendingChange(baker, resultBuilder));

            importState.NextPendingBakerChangeTime = await _writer.GetMinPendingChangeTime();
            _logger.Information("NextPendingBakerChangeTime set to {value}", importState.NextPendingBakerChangeTime);
        }
    }

    private void ApplyPendingChange(Baker baker, BakerUpdateResultsBuilder resultBuilder)
    {
        var activeState = baker.State as ActiveBakerState ?? throw new InvalidOperationException("Applying pending change to a baker that was not active!");
        if (activeState.PendingChange is PendingBakerRemoval pendingRemoval)
        {
            _logger.Information("Baker with id {bakerId} will be removed.", baker.Id);
            baker.State = new RemovedBakerState(pendingRemoval.EffectiveTime);
            resultBuilder.AddBakerRemoved(baker.Id);
        }
        else if (activeState.PendingChange is PendingBakerReduceStake reduceStake)
        {
            _logger.Information("Baker with id {bakerId} will have its stake reduced to {newStake}.", baker.Id, reduceStake.NewStakedAmount);
            activeState.PendingChange = null;
            activeState.StakedAmount = reduceStake.NewStakedAmount;
        }
        else throw new NotImplementedException("Applying this pending change is not implemented!");
    }

    private static Baker CreateNewBaker(ulong bakerId, CcdAmount stakedAmount, bool restakeEarnings, BakerPool? pool)
    {
        return new Baker
        {
            Id = (long)bakerId,
            State = new ActiveBakerState(stakedAmount.MicroCcdValue, restakeEarnings, pool, null)
        };
    }

    private BakerPool CreateDefaultBakerPool(BakerPoolOpenStatus openStatus = BakerPoolOpenStatus.ClosedForAll,
        decimal transactionCommission = 0.0m, decimal finalizationCommission = 0.0m, decimal bakingCommission = 0.0m)
    {
        return new BakerPool
        {
            OpenStatus = openStatus,
            MetadataUrl = "",
            CommissionRates = new CommissionRates
            {
                TransactionCommission = transactionCommission,
                FinalizationCommission = finalizationCommission,
                BakingCommission = bakingCommission
            },
            PaydayStatus = null,
        };
    }

    private static BakerPool GetPool(Baker dst)
    {
        var activeState = dst.State as ActiveBakerState ?? throw new InvalidOperationException("Cannot set open status for a baker that is not active!");
        return activeState.Pool ?? throw new InvalidOperationException("Cannot set open status for a baker where pool is null!");
    }

    private class BakerUpdateResultsBuilder
    {
        private ulong _totalAmountStaked = 0;
        private int _bakersAdded = 0;
        private readonly List<long> _bakersRemoved = new ();
        private readonly List<long> _bakersClosedForAll = new ();
        private PaydayPoolStakeSnapshot? _paydayStakeSnapshot = null;

        public void SetTotalAmountStaked(ulong totalAmountStaked)
        {
            _totalAmountStaked = totalAmountStaked;
        }

        public BakerUpdateResults Build()
        {
            return new BakerUpdateResults(_totalAmountStaked, _bakersAdded, _bakersRemoved.ToArray(),
                _bakersClosedForAll.ToArray(), _paydayStakeSnapshot);
        }

        public void IncrementBakersAdded(int incrementValue = 1)
        {
            _bakersAdded += incrementValue;
        }

        public void AddBakerRemoved(long bakerId)
        {
            _bakersRemoved.Add(bakerId);
        }

        public void AddBakerClosedForAll(long bakerId)
        {
            _bakersClosedForAll.Add(bakerId);
        }

        public void SetPaydayStakeSnapshot(PaydayPoolStakeSnapshot stakeSnapshot)
        {
            _paydayStakeSnapshot = stakeSnapshot;
        }
    }
}

public record BakerUpdateResults(ulong TotalAmountStaked,
    int BakersAddedCount,
    long[] BakerIdsRemoved,
    long[] BakerIdsClosedForAll,
    PaydayPoolStakeSnapshot? PaydayPoolStakeSnapshot)
{
    public int BakersRemovedCount => BakerIdsRemoved.Length;
};