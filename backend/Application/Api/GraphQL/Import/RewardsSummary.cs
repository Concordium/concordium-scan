using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import;

public class RewardsSummary
{
    public AccountRewardSummary[] AggregatedAccountRewards { get; }

    public RewardsSummary(AccountRewardSummary[] aggregatedAccountRewards)
    {
        AggregatedAccountRewards = aggregatedAccountRewards;
    }

    public static RewardsSummary Create(IList<ISpecialEvent> specialEvents, IAccountLookup accountLookup)
    {
        var rewards = specialEvents.SelectMany(se => se.GetAccountBalanceUpdates());

        var aggregatedRewards = rewards
            .Select(x => new
            {
                BaseAddress = x.AccountAddress.GetBaseAddress().ToString(),
                Amount = x.AmountAdjustment,
                x.BalanceUpdateType
            })
            .GroupBy(x => x.BaseAddress)
            .Select(addressGroup => new
            {
                BaseAddress = addressGroup.Key,
                TotalAmount = addressGroup.Aggregate(0L, (acc, item) => acc + item.Amount),
                TotalAmountByType = addressGroup
                    .GroupBy(x => x.BalanceUpdateType)
                    .Select(rewardTypeGroup =>
                        new RewardTypeAmount(rewardTypeGroup.Key.ToRewardType(),
                            rewardTypeGroup.Aggregate(0L, (acc, item) => acc + item.Amount))
                    )
                    .ToArray()
            })
            .ToArray();

        var baseAddresses = aggregatedRewards.Select(x => x.BaseAddress);
        var accountIdMap = accountLookup.GetAccountIdsFromBaseAddresses(baseAddresses);
        
        var accountRewards = aggregatedRewards
            .Select(x =>
            {
                var accountId = accountIdMap[x.BaseAddress] ?? throw new InvalidOperationException("Attempt at updating account that does not exist!");
                return new AccountRewardSummary(accountId, x.TotalAmount, x.TotalAmountByType);
            });

        return new RewardsSummary(accountRewards.ToArray());
    }
}

public record AccountRewardSummary(
    long AccountId, 
    long TotalAmount,
    RewardTypeAmount[] TotalAmountByType);

public record RewardTypeAmount(
    RewardType RewardType, 
    long TotalAmount);
