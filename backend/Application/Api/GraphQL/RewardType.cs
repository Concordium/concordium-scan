using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL;

public enum RewardType
{
    FinalizationReward = 6,
    FoundationReward = 7,
    BakerReward = 8,
    TransactionFeeReward = 9
}

public static class BalanceUpdateTypeExtensions
{
    public static RewardType ToRewardType(this BalanceUpdateType source)
    {
        return source switch
        {
            BalanceUpdateType.BakerReward => RewardType.BakerReward,
            BalanceUpdateType.FinalizationReward => RewardType.FinalizationReward,
            BalanceUpdateType.FoundationReward => RewardType.FoundationReward,
            BalanceUpdateType.TransactionFeeReward => RewardType.TransactionFeeReward,
            _ => throw new NotImplementedException()
        };
    }
}