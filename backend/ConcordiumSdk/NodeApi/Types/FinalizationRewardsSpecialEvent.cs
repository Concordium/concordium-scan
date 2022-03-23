using System.Linq;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class FinalizationRewardsSpecialEvent : SpecialEvent
{
    public AccountAddressAmount[] FinalizationRewards { get; init; }
    public CcdAmount Remainder { get; init; }
    
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return FinalizationRewards.Select(x => new AccountBalanceUpdate(x.Address, (long)x.Amount.MicroCcdValue, BalanceUpdateType.FinalizationReward));
    }
}