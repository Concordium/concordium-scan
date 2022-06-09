using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class PaydayFoundationRewardSpecialEvent : SpecialEvent
{
    public AccountAddress FoundationAccount { get; init; }
    public CcdAmount DevelopmentCharge { get; init; }

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        yield return new(FoundationAccount, (long)DevelopmentCharge.MicroCcdValue, BalanceUpdateType.FoundationReward);
    }
}