using Concordium.Sdk.Types;
using Concordium.Sdk.Types.New;

namespace Application.NodeApi;

public class PaydayFoundationRewardSpecialEvent : SpecialEvent
{
    public AccountAddress FoundationAccount { get; init; }
    public CcdAmount DevelopmentCharge { get; init; }

    public override IEnumerable<AccountBalanceUpdate> GetAccountBalanceUpdates()
    {
        yield return new(FoundationAccount, (long)DevelopmentCharge.Value, BalanceUpdateType.FoundationReward);
    }
}