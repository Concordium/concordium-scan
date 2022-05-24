using System.Threading.Tasks;
using System.Xml;
using Application.Api.GraphQL.Accounts;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.Import;

public class DelegationImportHandler
{
    private readonly AccountWriter _writer;
    private readonly ILogger _logger;

    public DelegationImportHandler(AccountWriter writer)
    {
        _writer = writer;
        _logger = Log.ForContext<DelegationImportHandler>();
    }

    public async Task<DelegationUpdateResults> HandleDelegationUpdates(BlockDataPayload payload, ChainParameters chainParameters,
        BakerUpdateResults bakerUpdateResults, RewardsSummary rewardsSummary, bool isFirstBlockAfterPayday)
    {
        var resultBuilder = new DelegationUpdateResultsBuilder();

        if (payload.BlockSummary.ProtocolVersion >= 4)
        {
            var chainParametersV1 = chainParameters as ChainParametersV1 ?? throw new InvalidOperationException("Chain parameters always expect to be v1 after protocol version 4");
    
            if (isFirstBlockAfterPayday)
                await _writer.UpdateAccountsWithPendingDelegationChange(payload.BlockInfo.BlockSlotTime,
                    account => ApplyPendingChange(account, resultBuilder));

            await HandleBakersRemovedOrClosedForAll(bakerUpdateResults, resultBuilder);
            
            var allTransactionEvents = payload.BlockSummary.TransactionSummaries
                .Select(tx => tx.Result).OfType<TransactionSuccessResult>()
                .SelectMany(x => x.Events)
                .ToArray();

            var txEvents = allTransactionEvents.Where(x => x
                is ConcordiumSdk.NodeApi.Types.DelegationAdded
                or ConcordiumSdk.NodeApi.Types.DelegationRemoved
                or ConcordiumSdk.NodeApi.Types.DelegationStakeIncreased
                or ConcordiumSdk.NodeApi.Types.DelegationStakeDecreased
                or ConcordiumSdk.NodeApi.Types.DelegationSetRestakeEarnings
                or ConcordiumSdk.NodeApi.Types.DelegationSetDelegationTarget);

            await UpdateDelegationFromTransactionEvents(txEvents, payload.BlockInfo, chainParametersV1, resultBuilder);
            await _writer.UpdateDelegationStakeIfRestakingEarnings(rewardsSummary.AggregatedAccountRewards);
            
            resultBuilder.SetTotalAmountStaked(await _writer.GetTotalDelegationAmountStaked());
        }

        return resultBuilder.Build();
    }

    private async Task HandleBakersRemovedOrClosedForAll(BakerUpdateResults bakerUpdateResults, DelegationUpdateResultsBuilder resultBuilder)
    {
        var bakerIds = bakerUpdateResults.BakerIdsRemoved
            .Concat(bakerUpdateResults.BakerIdsClosedForAll);
            
        foreach (var bakerId in bakerIds)
        {
            var target = new BakerDelegationTarget(bakerId);
            await _writer.UpdateAccounts(account => account.Delegation != null && account.Delegation.DelegationTarget == target, account =>
            {
                resultBuilder.DelegationTargetRemoved(account.Delegation!.DelegationTarget);
                account.Delegation!.DelegationTarget = new PassiveDelegationTarget();
                resultBuilder.DelegationTargetAdded(account.Delegation!.DelegationTarget);

            });
        }
    }

    private void ApplyPendingChange(Account account, DelegationUpdateResultsBuilder resultsBuilder)
    {
        var delegation = account.Delegation ?? throw new InvalidOperationException("Apply pending delegation change to an account that has no delegation!");
        if (delegation.PendingChange is PendingDelegationRemoval)
        {
            resultsBuilder.DelegationTargetRemoved(account.Delegation.DelegationTarget);
            account.Delegation = null;
        }
        else if (delegation.PendingChange is PendingDelegationReduceStake x)
        {
            delegation.StakedAmount = x.NewStakedAmount;
            delegation.PendingChange = null;
        }
    }

    private async Task UpdateDelegationFromTransactionEvents(IEnumerable<TransactionResultEvent> txEvents,
        BlockInfo blockInfo, ChainParametersV1 chainParameters, DelegationUpdateResultsBuilder resultBuilder)
    {
        foreach (var txEvent in txEvents)
        {
            if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationAdded added)
            {
                await _writer.UpdateAccount(added,
                    src => src.DelegatorId,
                    (_, dst) =>
                    {
                        if (dst.Delegation != null) throw new InvalidOperationException("Trying to add delegation to an account that already has a delegation set!");
                        dst.Delegation = new Delegation(0, false, new PassiveDelegationTarget());
                        resultBuilder.DelegationTargetAdded(dst.Delegation.DelegationTarget);
                    });
            }
            else if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationRemoved removed)
            {
                await _writer.UpdateAccount(removed,
                    src => src.DelegatorId,
                    (_, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                        var effectiveTime = blockInfo.BlockSlotTime.AddSeconds(chainParameters.DelegatorCooldown);
                        dst.Delegation.PendingChange = new PendingDelegationRemoval(effectiveTime);
                    });
            }
            else if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationStakeDecreased stakeDecreased)
            {
                await _writer.UpdateAccount(stakeDecreased,
                    src => src.DelegatorId,
                    (src, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                        var effectiveTime = blockInfo.BlockSlotTime.AddSeconds(chainParameters.DelegatorCooldown);
                        dst.Delegation.PendingChange = new PendingDelegationReduceStake(effectiveTime, stakeDecreased.NewStake.MicroCcdValue);
                    });
            }
            else if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationStakeIncreased stakeIncreased)
            {
                await _writer.UpdateAccount(stakeIncreased,
                    src => src.DelegatorId,
                    (src, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                        dst.Delegation.StakedAmount = src.NewStake.MicroCcdValue;
                    });
            }
            else if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationSetRestakeEarnings setRestakeEarnings)
            {
                await _writer.UpdateAccount(setRestakeEarnings,
                    src => src.DelegatorId,
                    (src, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set restake earnings flag on an account without a delegation instance!");
                        dst.Delegation.RestakeEarnings = src.RestakeEarnings;
                    });
            }
            else if (txEvent is ConcordiumSdk.NodeApi.Types.DelegationSetDelegationTarget setDelegationTarget)
            {
                await _writer.UpdateAccount(setDelegationTarget,
                    src => src.DelegatorId,
                    (src, dst) =>
                    {
                        if (dst.Delegation == null) throw new InvalidOperationException("Trying to set restake earnings flag on an account without a delegation instance!");
                        resultBuilder.DelegationTargetRemoved(dst.Delegation.DelegationTarget);
                        dst.Delegation.DelegationTarget = Map(src.DelegationTarget);
                        resultBuilder.DelegationTargetAdded(dst.Delegation.DelegationTarget);

                    });
            }
        }
    }

    private DelegationTarget Map(ConcordiumSdk.NodeApi.Types.DelegationTarget source)
    {
        return source switch
        {
            ConcordiumSdk.NodeApi.Types.PassiveDelegationTarget => new PassiveDelegationTarget(),
            ConcordiumSdk.NodeApi.Types.BakerDelegationTarget x => new BakerDelegationTarget((long)x.BakerId),
            _ => throw new NotImplementedException()
        };
    }

    public class DelegationUpdateResultsBuilder
    {
        private readonly List<DelegationTarget> _delegationTargetsRemoved = new ();
        private readonly List<DelegationTarget> _delegationTargetsAdded = new();
        private ulong _totalAmountStaked = 0;

        public void SetTotalAmountStaked(ulong totalAmountStaked)
        {
            _totalAmountStaked = totalAmountStaked;
        }

        public DelegationUpdateResults Build()
        {
            var adds = _delegationTargetsAdded.Select(x => new DelegationTargetDelegatorCountDelta(x, 1));
            var removes = _delegationTargetsRemoved.Select(x => new DelegationTargetDelegatorCountDelta(x, -1));

            var all = adds.Concat(removes)
                .GroupBy(x => x.DelegationTarget)
                .Select(x =>
                {
                    var delegatorCountDelta = x.Aggregate(0, (result, item) => result + item.DelegatorCountDelta);
                    return new DelegationTargetDelegatorCountDelta(x.Key, delegatorCountDelta);
                })
                .Where(x => x.DelegatorCountDelta != 0)
                .ToArray();
            
            return new DelegationUpdateResults(_totalAmountStaked, all);
        }

        public void DelegationTargetRemoved(DelegationTarget delegationTarget)
        {
            _delegationTargetsRemoved.Add(delegationTarget);
        }

        public void DelegationTargetAdded(DelegationTarget delegationTarget)
        {
            _delegationTargetsAdded.Add(delegationTarget);
        }
    }
}

public record DelegationUpdateResults(
    ulong TotalAmountStaked,
    DelegationTargetDelegatorCountDelta[] DelegatorCountDeltas);
    
public record DelegationTargetDelegatorCountDelta(
    DelegationTarget DelegationTarget, 
    int DelegatorCountDelta);