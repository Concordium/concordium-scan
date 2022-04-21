using ConcordiumSdk.Types;

namespace ConcordiumSdk.NodeApi.Types;

public class PaydayFoundationRewardSpecialEvent : SpecialEvent
{
    public AccountAddress FoundationAccount { get; init; }
    public CcdAmount DevelopmentCharge { get; init; }

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        return new AccountBalanceUpdate[]
        {
            new(FoundationAccount, (long)DevelopmentCharge.MicroCcdValue, BalanceUpdateType.BlockReward), // TODO: The type is off!
        };
    }
}