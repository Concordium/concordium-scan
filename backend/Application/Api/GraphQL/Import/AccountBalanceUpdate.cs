using System.Collections;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Import;

public sealed record AccountBalanceUpdateWithTransaction(AccountAddress AccountAddress,
    long AmountAdjustment,
    BalanceUpdateType BalanceUpdateType,
    TransactionHash TransactionHash) : AccountBalanceUpdate(AccountAddress, AmountAdjustment, BalanceUpdateType)
{

    internal static IEnumerable<AccountBalanceUpdateWithTransaction> From(
        IEnumerable<BlockItemSummary> blockItemSummaries)
    {
        foreach (var blockItemSummary in blockItemSummaries)
        {
            foreach (var accountBalanceUpdate in AccountBalanceUpdate.From(blockItemSummary))
            {
                yield return AccountBalanceUpdateWithTransaction.From(accountBalanceUpdate,
                    blockItemSummary.TransactionHash);
            }
        }
    }
    
    private static AccountBalanceUpdateWithTransaction From(AccountBalanceUpdate update, TransactionHash transactionHash)
    {
        return new AccountBalanceUpdateWithTransaction(
            update.AccountAddress,
            update.AmountAdjustment,
            update.BalanceUpdateType,
            transactionHash
        );
    }
}

public record AccountBalanceUpdate(
    AccountAddress AccountAddress,
    long AmountAdjustment,
    BalanceUpdateType BalanceUpdateType)
{

    internal static IEnumerable<AccountBalanceUpdate> From(IEnumerable<ISpecialEvent> specialEvents)
    {
        foreach (var specialEvent in specialEvents)
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
                    continue;
            }
        }
    }
    
    internal static IEnumerable<AccountBalanceUpdate> From(BlockItemSummary blockItemSummary)
    {
        switch (blockItemSummary.Details)
            {
                case AccountTransactionDetails accountTransactionDetails:
                    if (accountTransactionDetails.Cost > CcdAmount.Zero)
                    {
                        yield return new AccountBalanceUpdate(
                            accountTransactionDetails.Sender,
                            (long)accountTransactionDetails.Cost.Value,
                            BalanceUpdateType.TransactionFee
                        );
                    }
                    switch (accountTransactionDetails.Effects)
                    {
                        case AccountTransfer accountTransfer:
                            yield return new AccountBalanceUpdate(
                                accountTransactionDetails.Sender,
                                -1 * (long)accountTransfer.Amount.Value,
                                BalanceUpdateType.TransferOut
                            );
                            yield return new AccountBalanceUpdate(
                                accountTransfer.To,
                                (long)accountTransfer.Amount.Value,
                                BalanceUpdateType.TransferIn
                            );
                            break;
                        case ContractInitialized contractInitialized:
                            if (contractInitialized.Data.Amount > CcdAmount.Zero)
                            {
                                yield return new AccountBalanceUpdate(
                                    accountTransactionDetails.Sender,
                                    -1 * (long)contractInitialized.Data.Amount.Value,
                                    BalanceUpdateType.TransferOut
                                );
                            }
                            break;
                        case ContractUpdateIssued contractUpdateIssued:
                            foreach (var contractTraceElement in contractUpdateIssued.Effects)
                            {
                                switch (contractTraceElement)
                                {
                                    case Transferred transferred:
                                        yield return new AccountBalanceUpdate(
                                            transferred.To,
                                            (long)transferred.Amount.Value,
                                            BalanceUpdateType.TransferIn
                                        );
                                        break;
                                    case Updated updated:
                                        if (updated.Instigator is AccountAddress accountAddress)
                                        {
                                            yield return new AccountBalanceUpdate(
                                                accountAddress,
                                                -1 * (long)updated.Amount.Value,
                                                BalanceUpdateType.TransferOut
                                            );
                                        }
                                        break;
                                    case Resumed:
                                    case Upgraded:
                                    case Interrupted:
                                        continue;
                                }
                            }
                            break;
                        case TransferredToEncrypted transferredToEncrypted:
                            yield return new AccountBalanceUpdate(
                                accountTransactionDetails.Sender,
                                -1 * (long)transferredToEncrypted.Data.Amount.Value,
                                BalanceUpdateType.AmountEncrypted
                            );
                            break;
                        case TransferredToPublic transferredToPublic:
                            yield return new AccountBalanceUpdate(
                                accountTransactionDetails.Sender,
                                (long)transferredToPublic.Amount.Value,
                                BalanceUpdateType.AmountDecrypted
                            );
                            break;
                        case TransferredWithSchedule transferredWithSchedule:
                            var sum = transferredWithSchedule.Amount.Sum(a => (long)a.Item2.Value);
                            yield return new AccountBalanceUpdate(
                                accountTransactionDetails.Sender,
                                -1 * sum,
                                BalanceUpdateType.TransferOut
                            );
                            yield return new AccountBalanceUpdate(
                                transferredWithSchedule.To,
                                sum,
                                BalanceUpdateType.TransferIn
                            );
                            break;
                        case CredentialKeysUpdated:
                        case CredentialsUpdated:
                        case DataRegistered:
                        case DelegationConfigured:
                        case EncryptedAmountTransferred:
                        case ModuleDeployed:
                        case None:
                        case BakerAdded:
                        case BakerConfigured:
                        case BakerKeysUpdated:
                        case BakerRemoved:
                        case BakerRestakeEarningsUpdated:
                        case BakerStakeUpdated:
                            break;
                    }
                    break;
                case AccountCreationDetails:
                case UpdateDetails:
                    break;
            }
        }
}
