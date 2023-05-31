using Concordium.Sdk.Types;
using Concordium.Sdk.Types.New;

namespace Application.NodeApi;

public class BakingRewardsSpecialEvent : SpecialEvent
{
    public AccountAddressAmount[] BakerRewards { get; init; }
    public CcdAmount Remainder { get; init; }
    
    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return BakerRewards.Select(x => new AccountBalanceUpdate(x.Address, (long)x.Amount.Value, BalanceUpdateType.BakerReward));
    }
}