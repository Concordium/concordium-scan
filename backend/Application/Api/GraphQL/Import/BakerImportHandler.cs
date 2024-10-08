﻿using System.Threading.Tasks;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Application.Import;
using Application.NodeApi;
using Concordium.Sdk.Types;
using Microsoft.EntityFrameworkCore;
using Baker = Application.Api.GraphQL.Bakers.Baker;
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

    /// <summary>
    /// Process block data related to changes for bakers/validators.
    /// </summary>
    public async Task<BakerUpdateResults> HandleBakerUpdates(BlockDataPayload payload, RewardsSummary rewardsSummary,
        ChainParametersState chainParameters, BlockImportPaydayStatus importPaydayStatus, ImportState importState)
    {
        using var counter = _metrics.MeasureDuration(nameof(BakerImportHandler), nameof(HandleBakerUpdates));

        var changeStrategy = BakerChangeStrategyFactory.Create(payload.BlockInfo, chainParameters.Current, importPaydayStatus, _writer,
            payload.AccountInfos.BakersWithNewPendingChanges);;

        var resultBuilder = new BakerUpdateResultsBuilder();

        if (importPaydayStatus is FirstBlockAfterPayday)
        {
            var stakeSnapshot = await _writer.GetPaydayPoolStakeSnapshot();
            resultBuilder.SetPaydayStakeSnapshot(stakeSnapshot);
        }
        
        if (payload is GenesisBlockDataPayload)
            await AddGenesisBakers(payload, resultBuilder, importState);
        else
            await ApplyBakerChanges(payload, rewardsSummary, chainParameters, changeStrategy, importState, resultBuilder, importPaydayStatus is FirstBlockAfterPayday);
        
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
                var mapped = BakerTransactionRelation.TryFrom(tx, out var relation);
                return (mapped, relation);
            })
            .Where(x => x.mapped)
            .Select(x => x.relation!);

        await _writer.AddBakerTransactionRelations(items);
    }

    public async Task ApplyDelegationUpdates(BlockDataPayload payload, DelegationUpdateResults delegationUpdateResults,
        BakerUpdateResults bakerUpdateResults, ChainParameters chainParameters)
    {
        if (payload.BlockInfo.ProtocolVersion.AsInt() >= 4) // TODO: Could be optimized by only invoking on payday block (?)
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

        if (ChainParameters.TryGetCapitalBoundAndLeverageFactor(chainParameters, out var capitalBound, out var leverageFactor))
        {
            var totalAmountStaked = bakerUpdateResults.TotalAmountStaked + delegationUpdateResults.TotalAmountStaked;
            await _writer.UpdateDelegatedStakeCap(totalAmountStaked, capitalBound!.Value, leverageFactor!.AsDecimal());
        }
    }

    private async Task AddGenesisBakers(BlockDataPayload payload, BakerUpdateResultsBuilder resultBuilder, ImportState importState)
    {
        var mapBakerPool = payload.BlockInfo.ProtocolVersion.AsInt() >= 4;
        
        var genesisBakers = payload.AccountInfos.CreatedAccounts
            .Where(a => a.AccountStakingInfo != null)
            .Select(a => a.AccountStakingInfo!)
            .OfType<AccountBaker>()
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
        
        return Baker.CreateNewBaker(src.BakerInfo.BakerId, src.StakedAmount, src.RestakeEarnings, pool);
    }

    /// <summary>
    /// Map a <see cref="BakerPool"/> from a genesis block.
    ///
    /// Should only by called if <see cref="accountBaker"/> is an active baker. 
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown if <see cref="AccountBaker.BakerPoolInfo"/> is null since
    /// the <see cref="accountBaker"/> is expected to be a active baker.</exception>
    private static BakerPool MapBakerPool(AccountBaker accountBaker)
    {
        var poolInfo = accountBaker.BakerPoolInfo;
        if (poolInfo == null) throw new ArgumentNullException(nameof(accountBaker), "Did not expect baker pool info of the account to be null when trying to map it!");
        
        return new BakerPool
        {
            OpenStatus = poolInfo.OpenStatus.MapToGraphQlEnum(),
            MetadataUrl = poolInfo.MetadataUrl,
            CommissionRates = CommissionRates.From(poolInfo.CommissionRates),
            PaydayStatus = new CurrentPaydayStatus(accountBaker.StakedAmount, poolInfo.CommissionRates)
        };
    }

    private async Task ApplyBakerChanges(BlockDataPayload payload, RewardsSummary rewardsSummary,
        ChainParametersState chainParameters, IBakerChangeStrategy bakerChangeStrategy,
        ImportState importState, BakerUpdateResultsBuilder resultBuilder, bool isFirstBlockAfterPayday)
    {
        await MaybeMigrateToBakerPools(payload, importState);
        await MaybeApplyCommissionRangeChanges(chainParameters);
        await WorkAroundConcordiumNodeBug225(payload.BlockInfo, importState);
        
        await UpdateBakersWithPendingChangesDue(bakerChangeStrategy, importState, resultBuilder);

        var txEvents = payload.BlockItemSummaries
            .Where(b => b.IsSuccess())
            .Select(b => b.Details)
            .OfType<AccountTransactionDetails>()
            .Where(x => x.Effects
                is BakerAdded
                or BakerRemoved
                or BakerStakeUpdated
                or BakerRestakeEarningsUpdated
                or BakerConfigured
                or BakerKeysUpdated
            );

        await bakerChangeStrategy.UpdateBakersFromTransactionEvents(txEvents, importState, resultBuilder, payload.BlockInfo.BlockSlotTime);

        // This should happen after the bakers from current block has been added to the database
        if (isFirstBlockAfterPayday)
        {
            await UpdateCurrentPaydayStatusOnAllBakers(payload.ReadAllBakerPoolStatuses);
        }

        await _writer.UpdateStakeIfBakerActiveRestakingEarnings(rewardsSummary.AggregatedAccountRewards);
    }

    /// <summary>
    /// Updates <see cref="BakerPool.PaydayStatus"/> from <see cref="BakerPoolStatus"/> fetched from the node on all
    /// validators.
    /// </summary>
    internal async Task UpdateCurrentPaydayStatusOnAllBakers(Func<Task<BakerPoolStatus[]>> bakerPoolStatuses)
    {
        await _writer.CreateTemporaryBakerPoolPaydayStatuses();
        
        var poolStatuses = await bakerPoolStatuses();
        foreach (var poolStatus in poolStatuses)
        {
            await _writer.UpdateBaker(poolStatus, src => src.BakerId.Id.Index, (src, dst) =>
            {
                var pool = dst.ActiveState!.Pool ?? throw new InvalidOperationException("Did not expect this bakers pool property to be null");
                pool.ApplyPaydayStatus(src.CurrentPaydayStatus, src.PoolInfo.CommissionRates);
            });
        }
    }

    

    private async Task MaybeMigrateToBakerPools(BlockDataPayload payload, ImportState importState)
    {
        // Migrate to baker pool first time a block with protocol version 4 (or greater) is encountered.
        if (importState.MigrationToBakerPoolsCompleted || payload.BlockInfo.ProtocolVersion.AsInt() < 4)
            return;
        
        _logger.Information("Migrating all bakers to baker pools (protocol v4 update)...");

        var bakerPoolStatuses = await payload.ReadAllBakerPoolStatuses();
        var bakerPoolStatusesDict = bakerPoolStatuses
            .ToDictionary(x => (long)x.BakerId.Id.Index);
        
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
                        TransactionCommission = source.PoolInfo.CommissionRates.TransactionCommission.AsDecimal(),
                        FinalizationCommission = source.PoolInfo.CommissionRates.FinalizationCommission.AsDecimal(),
                        BakingCommission = source.PoolInfo.CommissionRates.BakingCommission.AsDecimal()
                    },
                    DelegatedStake = source.DelegatedCapital!.Value.Value,
                    DelegatorCount = 0,
                    DelegatedStakeCap = source.DelegatedCapitalCap!.Value.Value,
                    TotalStake = source.BakerEquityCapital!.Value.Value + source.DelegatedCapital.Value.Value
                };
                pool.ApplyPaydayStatus(source.CurrentPaydayStatus, source.PoolInfo.CommissionRates);
                
                baker.ActiveState!.Pool = pool;
            },
            baker => baker.ActiveState != null);
        
        importState.MigrationToBakerPoolsCompleted = true;
        _logger.Information("Migration completed!");
    }

    internal async Task MaybeApplyCommissionRangeChanges(ChainParametersState chainParametersState)
    {
        if(chainParametersState is ChainParametersChangedState changedState &&
            ChainParameters.TryGetCommissionRanges(changedState.Current,
               out var currentFinalizationCommissionRange,
               out var currentBakingCommissionRange,
               out var currentTransactionCommissionRange) && 
           ChainParameters.TryGetCommissionRanges(changedState.Previous,
               out var previousFinalizationCommissionRange,
               out var previousBakingCommissionRange,
               out var previousTransactionCommissionRange))
        {
            if (currentFinalizationCommissionRange!.Equals(previousFinalizationCommissionRange)
                && currentBakingCommissionRange!.Equals(previousBakingCommissionRange)
                && currentTransactionCommissionRange!.Equals(previousTransactionCommissionRange))
                return; // No commission ranges changed!

            _logger.Information("Applying commission range changes to baker pools");

            await _writer.UpdateBakers(baker =>
                {
                    var rates = baker.ActiveState!.Pool!.CommissionRates;
                    rates.FinalizationCommission = AdjustValueToRange(rates.FinalizationCommission, currentFinalizationCommissionRange);
                    rates.BakingCommission = AdjustValueToRange(rates.BakingCommission, currentBakingCommissionRange!);
                    rates.TransactionCommission = AdjustValueToRange(rates.TransactionCommission, currentTransactionCommissionRange!);
                },
                baker => baker.ActiveState!.Pool != null);

            _logger.Information("Commission range changed applied!");
        }
    }

    private static decimal AdjustValueToRange(decimal currentValue, CommissionRange allowedRange)
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

            importState.LastGenesisIndex = (int)blockInfo.GenesisIndex;

            var networkId = ConcordiumNetworkId.TryGetFromGenesisBlockHash(Concordium.Sdk.Types.BlockHash.From(importState.GenesisBlockHash));
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

    private async Task UpdateBakersWithPendingChangesDue(IBakerChangeStrategy bakerChangeStrategy,
        ImportState importState, BakerUpdateResultsBuilder resultBuilder)
    {
        // Check if this protocol supports pending changes.
        if (bakerChangeStrategy.SupportsPendingChanges()) {
            if (bakerChangeStrategy.MustApplyPendingChangesDue(importState.NextPendingBakerChangeTime))
            {
                var effectiveTime = bakerChangeStrategy.GetEffectiveTime();
                await _writer.UpdateBakersWithPendingChange(effectiveTime, baker => ApplyPendingChange(baker, resultBuilder));

                importState.NextPendingBakerChangeTime = await _writer.GetMinPendingChangeTime();
                _logger.Information("NextPendingBakerChangeTime set to {value}", importState.NextPendingBakerChangeTime);
            }
        } else {
            // Starting from protocol version 7 and onwards stake changes are immediate, so we apply all of them in the first block of P7 and this is a no-op for future blocks.
            await _writer.UpdateBakersWithPendingChange(DateTimeOffset.MaxValue, baker => ApplyPendingChange(baker, resultBuilder));
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

    public class BakerUpdateResultsBuilder
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
