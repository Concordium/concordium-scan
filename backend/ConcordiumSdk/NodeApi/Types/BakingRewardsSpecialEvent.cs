using System.Linq;
using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class BakingRewardsSpecialEvent : SpecialEvent
{
    public AccountAddressAmount[] BakerRewards { get; init; }
    public CcdAmount Remainder { get; init; }
    
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return BakerRewards.Select(x => new AccountBalanceUpdate(x.Address, (long)x.Amount.MicroCcdValue, BalanceUpdateType.BakerReward));
    }
}