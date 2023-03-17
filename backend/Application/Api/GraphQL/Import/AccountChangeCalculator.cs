using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Common.Diagnostics;
using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.Import;

public class AccountChangeCalculator
{
    private readonly IAccountLookup _accountLookup;
    private readonly IMetrics _metrics;

    public AccountChangeCalculator(IAccountLookup accountLookup, IMetrics metrics)
    {
        _accountLookup = accountLookup;
        _metrics = metrics;
    }

    /// <summary>
    /// This Method should only be used to Map Newly Created Accounts.
    /// Since Balance of the Account is not taken into consideration
    /// </summary>
    /// <param name="createdAccounts">Created Account</param>
    /// <param name="blockSlotTime">Block Slot Time</param>
    /// <returns></returns>
    public IEnumerable<Account> MapCreatedAccounts(
        AccountInfo[] createdAccounts, 
        DateTimeOffset blockSlotTime, 
        int blockHeight)
    {
        return createdAccounts.Select(x => new Account
        {
            Id = (long)x.AccountIndex,
            CanonicalAddress = new AccountAddress(x.AccountAddress.AsString),
            BaseAddress = new AccountAddress(x.AccountAddress.GetBaseAddress().AsString),
            //Newly Created Account should not have balance. Balance will be later computer through transactions
            Amount = blockHeight == 0 ? x.AccountAmount.MicroCcdValue : 0, 
            CreatedAt = blockSlotTime
        });
    }

    public AccountTransactionRelation[] GetAccountTransactionRelations(TransactionPair[] transactions)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountChangeCalculator), nameof(GetAccountTransactionRelations));

        var result = Array.Empty<AccountTransactionRelation>();

        var accountTransactions = transactions
            .Select(x => new
            {
                TransactionId = x.Target.Id,
                DistinctAccountBaseAddresses = FindAccountAddresses(x.Source)
                    .Select(address => address.GetBaseAddress())
                    .Distinct()
            })
            .SelectMany(x => x.DistinctAccountBaseAddresses
                .Select(accountBaseAddress => new
                {
                    AccountBaseAddress = accountBaseAddress.AsString,
                    x.TransactionId
                }))
            .ToArray();

        if (accountTransactions.Length > 0)
        {
            var distinctBaseAddresses = accountTransactions
                .Select(x => x.AccountBaseAddress)
                .Distinct();
            var accountIdLookup = _accountLookup.GetAccountIdsFromBaseAddresses(distinctBaseAddresses);

            result = accountTransactions
                .Select(x =>
                {
                    var accountId = accountIdLookup[x.AccountBaseAddress];
                    if (accountId.HasValue)
                        return new AccountTransactionRelation
                        {
                            AccountId = accountId.Value,
                            TransactionId = x.TransactionId
                        };
                    return null;
                })
                .Where(x => x != null)
                .ToArray()!;
        }

        return result;
    }
    
    private IEnumerable<ConcordiumSdk.Types.AccountAddress> FindAccountAddresses(TransactionSummary source)
    {
        if (source.Sender != null) yield return source.Sender;
        foreach (var address in source.Result.GetAccountAddresses())
            yield return address;
    }

    public AccountUpdate[] GetAggregatedAccountUpdates(IEnumerable<AccountBalanceUpdate> balanceUpdates, AccountTransactionRelation[] transactionRelations)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountChangeCalculator), nameof(GetAggregatedAccountUpdates));

        var aggregatedBalanceUpdates = balanceUpdates
            .Select(x => new { BaseAddress = x.AccountAddress.GetBaseAddress().AsString, x.AmountAdjustment })
            .GroupBy(x => x.BaseAddress)
            .Select(addressGroup => new
            {
                BaseAddress = addressGroup.Key,
                AmountAdjustment = addressGroup.Aggregate(0L, (acc, item) => acc + item.AmountAdjustment)
            })
            .ToArray();

        var baseAddresses = aggregatedBalanceUpdates.Select(x => x.BaseAddress);
        var accountIdMap = _accountLookup.GetAccountIdsFromBaseAddresses(baseAddresses);
        
        var amountAdjustmentResults = aggregatedBalanceUpdates
            .Select(x =>
            {
                var accountId = accountIdMap[x.BaseAddress] ?? throw new InvalidOperationException("Attempt at updating account that does not exist!");
                return new AccountUpdate(accountId, x.AmountAdjustment, 0);
            });

        var transactionResults = transactionRelations
            .GroupBy(x => x.AccountId)
            .Select(x => new { AccountId = x.Key, TransactionsAdded = x.Aggregate(0, (acc, _) => ++acc) })
            .Select(x => new AccountUpdate(x.AccountId, 0, x.TransactionsAdded));

        return amountAdjustmentResults.Concat(transactionResults)
            .GroupBy(x => x.AccountId)
            .Select(group => new AccountUpdate(@group.Key,
                @group.Aggregate(0L, (acc, item) => acc + item.AmountAdjustment),
                @group.Aggregate(0, (acc, item) => acc + item.TransactionsAdded)))
            .ToArray();
    }

    public AccountReleaseScheduleItem[] GetAccountReleaseScheduleItems(IEnumerable<TransactionPair> transactions)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountChangeCalculator), nameof(GetAccountReleaseScheduleItems));

        AccountReleaseScheduleItem[] toInsert = Array.Empty<AccountReleaseScheduleItem>();

        var result = transactions
            .Where(transaction => transaction.Source.Result is TransactionSuccessResult)
            .SelectMany(transaction =>
            {
                return ((TransactionSuccessResult)transaction.Source.Result).Events
                    .OfType<ConcordiumSdk.NodeApi.Types.TransferredWithSchedule>()
                    .SelectMany(scheduleEvent => scheduleEvent.Amount.Select((amount, ix) => new
                    {
                        AccountBaseAddress = scheduleEvent.To.GetBaseAddress().AsString,
                        TransactionId = transaction.Target.Id,
                        ScheduleIndex = ix,
                        Timestamp = amount.Timestamp,
                        Amount = Convert.ToUInt64(amount.Amount.MicroCcdValue),
                        FromAccountBaseAddress = scheduleEvent.From.GetBaseAddress().AsString
                    }));
            }).ToArray();

        if (result.Length > 0)
        {
            var distinctBaseAddresses = result
                .Select(x => x.AccountBaseAddress)
                .Concat(result.Select(x => x.FromAccountBaseAddress))
                .Distinct();
            var accountIdMap = _accountLookup.GetAccountIdsFromBaseAddresses(distinctBaseAddresses);

            toInsert = result.Select(x => new AccountReleaseScheduleItem
                {
                    AccountId = accountIdMap[x.AccountBaseAddress] ?? throw new InvalidOperationException("Account does not exist!"),
                    TransactionId = x.TransactionId,
                    Index = x.ScheduleIndex,
                    Timestamp = x.Timestamp,
                    Amount = x.Amount,
                    FromAccountId = accountIdMap[x.FromAccountBaseAddress] ?? throw new InvalidOperationException("Account does not exist!"),
                })
                .ToArray();
        }

        return toInsert;
    }

    public IEnumerable<AccountStatementEntry> GetAccountStatementEntries(
        AccountBalanceUpdate[] balanceUpdates, AccountUpdateResult[] accountUpdateResults, Block block, TransactionPair[] transactions)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountChangeCalculator), nameof(GetAccountStatementEntries));

        var distinctBaseAddresses = balanceUpdates
            .Select(x => x.AccountAddress.GetBaseAddress().AsString)
            .Distinct();
        var accountIdMap = _accountLookup.GetAccountIdsFromBaseAddresses(distinctBaseAddresses);

        var result = balanceUpdates.Select(x => new AccountStatementEntry
            {
                AccountId = accountIdMap[x.AccountAddress.GetBaseAddress().AsString] ?? throw new InvalidOperationException("Account not found!"),
                Index = default, // Will be set by database
                Timestamp = block.BlockSlotTime,
                Amount = x.AmountAdjustment,
                EntryType = Map(x.BalanceUpdateType),
                BlockId = block.Id,
                TransactionId = GetTransactionId(x, transactions)
            })
            .ToArray();

        var accountUpdateResultsDictionary = accountUpdateResults.ToDictionary(x => x.AccountId);
        foreach (var accountGroup in result.GroupBy(x => x.AccountId))
        {
            var updateResult = accountUpdateResultsDictionary[accountGroup.Key];
            
            var accountBalance = updateResult.AccountBalanceBeforeUpdate;
            foreach (var entry in accountGroup)
            {
                accountBalance = (ulong)((long)accountBalance + entry.Amount);
                entry.AccountBalance = accountBalance;
            }

            if (accountBalance != updateResult.AccountBalanceAfterUpdate)
                throw new InvalidOperationException("Did not end up with the expected result!");
        }
        
        return result;
    }

    private long? GetTransactionId(AccountBalanceUpdate update, TransactionPair[] transactions)
    {
        if (update.TransactionHash == null) return null;
        
        var transaction = transactions.Single(x => x.Source.Hash == update.TransactionHash!);
        return transaction.Target.Id;
    }

    private AccountStatementEntryType Map(BalanceUpdateType input)
    {
        return input switch
        {
            BalanceUpdateType.AmountDecrypted => AccountStatementEntryType.AmountDecrypted,
            BalanceUpdateType.AmountEncrypted => AccountStatementEntryType.AmountEncrypted,
            BalanceUpdateType.BakerReward => AccountStatementEntryType.BakerReward,
            BalanceUpdateType.TransactionFeeReward => AccountStatementEntryType.TransactionFeeReward,
            BalanceUpdateType.FinalizationReward => AccountStatementEntryType.FinalizationReward,
            BalanceUpdateType.FoundationReward => AccountStatementEntryType.FoundationReward,
            BalanceUpdateType.TransactionFee => AccountStatementEntryType.TransactionFee,
            BalanceUpdateType.TransferIn => AccountStatementEntryType.TransferIn,
            BalanceUpdateType.TransferOut => AccountStatementEntryType.TransferOut,
            _ => throw new NotImplementedException()
        };
    }
}
