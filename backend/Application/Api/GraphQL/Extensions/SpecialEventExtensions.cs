using Application.Api.GraphQL.Import;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Extensions;

internal static class SpecialEventExtensions
{
    internal static IEnumerable<AccountBalanceUpdate> Into(this ISpecialEvent specialEvent)
    {
        switch (specialEvent)
            {
                case BakingRewards bakingRewards:
                    foreach (var (accountAddress, ccdAmount) in bakingRewards.Rewards)
                    {
                        yield return new AccountBalanceUpdate(accountAddress, (long)ccdAmount.Value,
                            BalanceUpdateType.BakerReward);
                    }
                    break;
                case BlockReward blockReward:
                    if (blockReward.FoundationCharge > CcdAmount.Zero)
                    {
                        yield return new AccountBalanceUpdate(blockReward.FoundationAccount, (long)blockReward.FoundationCharge.Value,
                            BalanceUpdateType.FoundationReward);
                    }
                    if (blockReward.BakerReward > CcdAmount.Zero)
                    {
                        yield return new AccountBalanceUpdate(blockReward.Baker, (long)blockReward.BakerReward.Value,
                            BalanceUpdateType.TransactionFeeReward);
                    }
                    break;
                case FinalizationRewards finalizationRewards:
                    foreach (var (accountAddress, ccdAmount) in finalizationRewards.Rewards)
                    {
                        yield return new AccountBalanceUpdate(accountAddress, (long)ccdAmount.Value, BalanceUpdateType.FinalizationReward);
                    }
                    break;
                case Mint mint:
                    yield return new AccountBalanceUpdate(mint.FoundationAccount, (long)mint.MintPlatformDevelopmentCharge.Value,
                        BalanceUpdateType.FoundationReward);
                    break;
                case PaydayAccountReward paydayAccountReward:
                    yield return new AccountBalanceUpdate(paydayAccountReward.Account, (long)paydayAccountReward.TransactionFees.Value, BalanceUpdateType.TransactionFeeReward);
                    yield return new AccountBalanceUpdate(paydayAccountReward.Account, (long)paydayAccountReward.BakerReward.Value, BalanceUpdateType.BakerReward);
                    yield return new AccountBalanceUpdate(paydayAccountReward.Account, (long)paydayAccountReward.FinalizationReward.Value, BalanceUpdateType.FinalizationReward);
                    break;
                case PaydayFoundationReward paydayFoundationReward:
                    yield return new AccountBalanceUpdate(paydayFoundationReward.FoundationAccount, (long)paydayFoundationReward.DevelopmentCharge.Value,
                        BalanceUpdateType.FoundationReward);
                    break;
                case PaydayPoolReward:
                case BlockAccrueReward:
                    break;
            }
    }

}