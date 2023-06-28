using System.Collections;
using Application.Api.GraphQL.Extensions;
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
            foreach (var accountBalanceUpdate in blockItemSummary.Into())
            {
                yield return From(accountBalanceUpdate,
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
            foreach (var accountBalanceUpdate in specialEvent.Into())
            {
                yield return accountBalanceUpdate;
            }
        }
    }
}
