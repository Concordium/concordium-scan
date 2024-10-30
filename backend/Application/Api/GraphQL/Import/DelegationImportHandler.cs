using System.Threading.Tasks;
using Application.Api.GraphQL.Accounts;
using Application.Import;
using Concordium.Sdk.Types;

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

    /// <summary>
    /// Process delegation related information from a block.
    /// </summary>
    public async Task<DelegationUpdateResults> HandleDelegationUpdates(BlockDataPayload payload,
        ChainParameters chainParameters, BakerUpdateResults bakerUpdateResults, RewardsSummary rewardsSummary,
        BlockImportPaydayStatus importPaydayStatus)
    {
        var resultBuilder = new DelegationUpdateResultsBuilder();

        // Delegation was introduced as part of Concordium Protocol Version 4,
        // meaning we can just return for blocks from a protocol version prior to that.
        if (payload.BlockInfo.ProtocolVersion < ProtocolVersion.P4) return resultBuilder.Build();

        if (payload.BlockInfo.ProtocolVersion < ProtocolVersion.P7 && importPaydayStatus is FirstBlockAfterPayday firstBlockAfterPayday) {
            // Handle effective pending changes for delegators and update the resultBuilder.
            // Stake changes only take effect from the first block in each payday.
            await _writer.UpdateAccountsWithPendingDelegationChange(firstBlockAfterPayday.PaydayTimestamp,
                account => ApplyPendingChange(account, resultBuilder));

        } else if (payload.BlockInfo.ProtocolVersion == ProtocolVersion.P7) {
            // Starting from Concordium Protocol Version 7 stake changes are immediate,
            // meaning no delegators are expected to have pending changes from this point.
            // Only the block in P7 should this do anything, afterwards it is a
            // no-op, since no accounts with pending changes are expected.
            await _writer.UpdateAccountsWithPendingDelegationChange(DateTimeOffset.MaxValue,
                account => ApplyPendingChange(account, resultBuilder));

        }

        // Handle delegation state changes due to pools that are either removed or closed for delegation.
        await HandleBakersRemovedOrClosedForAll(bakerUpdateResults, resultBuilder);

        var delegationConfigureEvents = payload.BlockItemSummaries.Where(b => b.IsSuccess())
            .Select(b => b.Details as AccountTransactionDetails)
            .Where(atd => atd is not null)
            .Select(atd => atd!.Effects as DelegationConfigured)
            .Where(d => d is not null)
            .Select(d => d!);

        // Get the current cooldown parameter for delegation.
        if (!ChainParameters.TryGetDelegatorCooldown(chainParameters, out var delegatorCooldownOut))
        {
            throw new InvalidOperationException("Delegator cooldown expected for protocol version 4 and above");
        }
        var delegatorCooldown = delegatorCooldownOut!.Value; // Safe since we throw for the failing case above.

        await UpdateDelegationFromTransactionEvents(delegationConfigureEvents, payload.BlockInfo, delegatorCooldown, resultBuilder);
        await _writer.UpdateDelegationStakeIfRestakingEarnings(rewardsSummary.AggregatedAccountRewards);

        resultBuilder.SetTotalAmountStaked(await _writer.GetTotalDelegationAmountStaked());

        return resultBuilder.Build();
    }

    /// <summary>
    /// Iterate removed and closed pools, moves the delegators to target the passive pool and updates the account delegation information and updates <paramref name="resultBuilder"/>.
    /// </summary>
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

    /// <summary>
    /// Update/Apply delegation state on the <paramref name="account"/> with the pending change.
    /// Records the stake change in <paramref name="resultsBuilder"/>.
    /// Throws if <paramref name="account"/> is not delegating.
    /// </summary>
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

    private async Task UpdateDelegationFromTransactionEvents(IEnumerable<DelegationConfigured> delegations,
        BlockInfo blockInfo, ulong delegatorCooldown, DelegationUpdateResultsBuilder resultBuilder)
    {
        foreach (var configured in delegations)
        {
            foreach (var delegation in configured.Data)
            {
                switch (delegation)
                {
                    case DelegationAdded delegationAdded:
                        await _writer.UpdateAccount(delegationAdded,
                            src => src.DelegatorId.Id.Index,
                            (_, dst) =>
                            {
                                if (dst.Delegation != null) throw new InvalidOperationException("Trying to add delegation to an account that already has a delegation set!");
                                dst.Delegation = new Delegation(0, false, new PassiveDelegationTarget());
                                resultBuilder.DelegationTargetAdded(dst.Delegation.DelegationTarget);
                            });
                        break;
                    case DelegationRemoved delegationRemoved:
                        await _writer.UpdateAccount(delegationRemoved,
                            src => src.DelegatorId.Id.Index,
                            (_, dst) =>
                            {
                                if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                                if (blockInfo.ProtocolVersion < ProtocolVersion.P7) {
                                    var effectiveTime = blockInfo.BlockSlotTime.AddSeconds(delegatorCooldown);
                                    dst.Delegation.PendingChange = new PendingDelegationRemoval(effectiveTime);
                                } else {
                                    resultBuilder.DelegationTargetRemoved(dst.Delegation.DelegationTarget);
                                    dst.Delegation = null;
                                }
                            });
                        break;
                    case DelegationSetDelegationTarget delegationSetDelegationTarget:
                        await _writer.UpdateAccount(delegationSetDelegationTarget,
                            src => src.DelegatorId.Id.Index,
                            (src, dst) =>
                            {
                                if (dst.Delegation == null) throw new InvalidOperationException("Trying to set restake earnings flag on an account without a delegation instance!");
                                resultBuilder.DelegationTargetRemoved(dst.Delegation.DelegationTarget);
                                dst.Delegation.DelegationTarget = DelegationTarget.From(delegationSetDelegationTarget.DelegationTarget);
                                resultBuilder.DelegationTargetAdded(dst.Delegation.DelegationTarget);

                            });
                        break;
                    case DelegationSetRestakeEarnings delegationSetRestakeEarnings:
                        await _writer.UpdateAccount(delegationSetRestakeEarnings,
                            src => src.DelegatorId.Id.Index,
                            (src, dst) =>
                            {
                                if (dst.Delegation == null) throw new InvalidOperationException("Trying to set restake earnings flag on an account without a delegation instance!");
                                dst.Delegation.RestakeEarnings = src.RestakeEarnings;
                            });
                        break;
                    case DelegationStakeDecreased delegationStakeDecreased:
                        await _writer.UpdateAccount(delegationStakeDecreased,
                            src => src.DelegatorId.Id.Index,
                            (src, dst) =>
                            {
                                if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                                if (blockInfo.ProtocolVersion < ProtocolVersion.P7) {
                                    var effectiveTime = blockInfo.BlockSlotTime.AddSeconds(delegatorCooldown);
                                    dst.Delegation.PendingChange = new PendingDelegationReduceStake(effectiveTime, delegationStakeDecreased.NewStake.Value);
                                } else {
                                    dst.Delegation.StakedAmount = src.NewStake.Value;
                                }
                            });
                        break;
                    case DelegationStakeIncreased delegationStakeIncreased:
                        await _writer.UpdateAccount(delegationStakeIncreased,
                            src => src.DelegatorId.Id.Index,
                            (src, dst) =>
                            {
                                if (dst.Delegation == null) throw new InvalidOperationException("Trying to set pending change to remove delegation on an account without a delegation instance!");
                                dst.Delegation.StakedAmount = src.NewStake.Value;
                            });
                        break;
                    case DelegationEventBakerRemoved bakerRemoved:
                        // We can update the database immediately and without tracking a pending change
                        // because this event was introduced as part of Protocol Version 7, where
                        // pending changes are also removed.
                        var delegationsMoveToPassive = await _writer.RemoveBaker(bakerRemoved.BakerId, blockInfo.BlockSlotTime);
                        // Update the resultsBuilder with the moved delegations.
                        var bakerTarget = new BakerDelegationTarget((long)bakerRemoved.BakerId.Id.Index);
                        var passiveTarget = new PassiveDelegationTarget();
                        for (int i = 0; i < delegationsMoveToPassive; i++)
                        {
                            resultBuilder.DelegationTargetRemoved(bakerTarget);
                            resultBuilder.DelegationTargetAdded(passiveTarget);
                        }
                        break;
                }

            }
        }
    }

    /// <summary>
    /// Tracker of stake changes due to removed or reduced stake by delegators.
    /// Later this will be used to update the stake of pools.
    /// </summary>
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