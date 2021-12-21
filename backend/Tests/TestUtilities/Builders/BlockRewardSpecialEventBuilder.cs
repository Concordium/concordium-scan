using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;

namespace Tests.TestUtilities.Builders;

public class BlockRewardSpecialEventBuilder
{
    public BlockRewardSpecialEvent Build()
    {
        return new BlockRewardSpecialEvent()
        {
            BakerReward = CcdAmount.FromMicroCcd(5111884),
            FoundationCharge = CcdAmount.FromMicroCcd(4884),
            TransactionFees = CcdAmount.FromMicroCcd(8888),
            NewGasAccount = CcdAmount.FromMicroCcd(455),
            OldGasAccount = CcdAmount.FromMicroCcd(22),
            Baker = new AccountAddress("31JA2dWnv6xHrdP73kLKvWqr5RMfqoeuJXG2Mep1iyQV9E5aSd"),
            FoundationAccount = new AccountAddress("3rsc7HNLVKnFz9vmKkAaEMVpNkFA4hZxJpZinCtUTJbBh58yYi")
        };
    }
}