using Application.Api.GraphQL.Import;
using Concordium.Sdk.Types;

namespace Application.Api.GraphQL.Extensions;

internal static class BlockItemSummaryExtensions
{
    internal static IEnumerable<AccountBalanceUpdate> Into(this BlockItemSummary blockItemSummary)
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

    internal static IEnumerable<BakerId> GetBakerIds(this BlockItemSummary blockItemSummary)
    {
        switch (blockItemSummary.Details)
            {
                case AccountTransactionDetails accountTransactionDetails:
                    switch (accountTransactionDetails.Effects)
                    {
                        case BakerAdded bakerAdded:
                            yield return bakerAdded.KeysEvent.BakerId;
                            break;
                        case BakerConfigured bakerConfigured:
                            foreach (var bakerId in bakerConfigured.GetBakerIds())
                            {
                                yield return bakerId;
                            }
                            break;
                        case BakerKeysUpdated bakerKeysUpdated:
                            yield return bakerKeysUpdated.KeysEvent.BakerId;
                            break;
                        case BakerRemoved bakerRemoved:
                            yield return bakerRemoved.BakerId;
                            break;
                        case BakerRestakeEarningsUpdated bakerRestakeEarningsUpdated:
                            yield return bakerRestakeEarningsUpdated.BakerId;
                            break;
                        case BakerStakeUpdated bakerStakeUpdated:
                            if (bakerStakeUpdated.Data is not null)
                            {
                                yield return bakerStakeUpdated.Data.BakerId;
                            }
                            break;
                        case ContractInitialized:
                        case ContractUpdateIssued:
                        case CredentialKeysUpdated:
                        case CredentialsUpdated:
                        case DataRegistered:
                        case DelegationConfigured:
                        case EncryptedAmountTransferred:
                        case ModuleDeployed:
                        case None:
                        case TransferredToEncrypted:
                        case TransferredToPublic:
                        case TransferredWithSchedule:
                        case AccountTransfer:
                            break;
                    }
                    break;
                case AccountCreationDetails:
                case UpdateDetails:
                    break;
            }
    }
}