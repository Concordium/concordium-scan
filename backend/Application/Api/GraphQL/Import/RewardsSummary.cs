using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.Import;

public class RewardsSummary
{
    public AccountReward[] AggregatedAccountRewards { get; }

    public RewardsSummary(AccountReward[] aggregatedAccountRewards)
    {
        AggregatedAccountRewards = aggregatedAccountRewards;
    }

    public static RewardsSummary Create(BlockSummaryBase blockSummary, IAccountLookup accountLookup)
    {
        var rewards = blockSummary.SpecialEvents.SelectMany(se => se.GetAccountBalanceUpdates());
        
        var aggregatedRewards = rewards
            .Select(x => new { BaseAddress = x.AccountAddress.GetBaseAddress().AsString, Amount = x.AmountAdjustment })
            .GroupBy(x => x.BaseAddress)
            .Select(addressGroup => new
            {
                BaseAddress = addressGroup.Key,
                Amount = addressGroup.Aggregate(0L, (acc, item) => acc + item.Amount)
            })
            .ToArray();

        var baseAddresses = aggregatedRewards.Select(x => x.BaseAddress);
        var accountIdMap = accountLookup.GetAccountIdsFromBaseAddresses(baseAddresses);
        
        var accountRewards = aggregatedRewards
            .Select(x =>
            {
                var accountId = accountIdMap[x.BaseAddress] ?? throw new InvalidOperationException("Attempt at updating account that does not exist!");
                return new AccountReward(accountId, x.Amount);
            });

        return new RewardsSummary(accountRewards.ToArray());
    }
}

public record AccountReward(long AccountId, long RewardAmount);
