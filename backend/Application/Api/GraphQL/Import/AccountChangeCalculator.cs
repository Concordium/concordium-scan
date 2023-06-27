using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Common.Diagnostics;
using Concordium.Sdk.Types;
using AccountAddress = Application.Api.GraphQL.Accounts.AccountAddress;

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
    /// <param name="blockHeight">Block height from genesis</param>
    /// <returns></returns>
    public IEnumerable<Account> MapCreatedAccounts(
        AccountInfo[] createdAccounts, 
        DateTimeOffset blockSlotTime, 
        ulong blockHeight)
    {
        return createdAccounts.Select(x => new Account
        {
            Id = (long)x.AccountIndex.Index,
            CanonicalAddress = new AccountAddress(x.AccountAddress.ToString()),
            BaseAddress = new AccountAddress(x.AccountAddress.GetBaseAddress().ToString()),
            //Newly Created Account should not have balance. Balance will be later computer through transactions
            Amount = blockHeight == 0 ? x.AccountAmount.Value : 0, 
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
                DistinctAccountBaseAddresses = x.Source.AffectedAddresses()
                    .Select(address => address.GetBaseAddress())
                    .Distinct()
            })
            .SelectMany(x => x.DistinctAccountBaseAddresses
                .Select(accountBaseAddress => new
                {
                    AccountBaseAddress = accountBaseAddress.ToString(),
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

    public AccountUpdate[] GetAggregatedAccountUpdates(IEnumerable<AccountBalanceUpdate> balanceUpdates, AccountTransactionRelation[] transactionRelations)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountChangeCalculator), nameof(GetAggregatedAccountUpdates));

        var aggregatedBalanceUpdates = balanceUpdates
            .Select(x => new { BaseAddress = x.AccountAddress.GetBaseAddress().ToString(), x.AmountAdjustment })
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

        var toInsert = Array.Empty<AccountReleaseScheduleItem>();
        var events = new List<ScheduleHolder>();
        
        foreach (var (blockItemSummary, transaction) in transactions
                     .Where(t => t.Source.Details is AccountTransactionDetails)
                     .Where(t => ((AccountTransactionDetails)t.Source.Details).Effects is TransferredWithSchedule))
        {
            if (blockItemSummary.Details is not AccountTransactionDetails accountTransactionDetails)
            {
                continue;
            }

            if (accountTransactionDetails.Effects is not TransferredWithSchedule scheduleEvent)
            {
                continue;
            }
            events.AddRange(
                scheduleEvent.Amount.Select((amount, ix) => new ScheduleHolder(
                ToAccountBaseAddress: scheduleEvent.To.GetBaseAddress().ToString(),
                TransactionId: transaction.Id,
                ScheduleIndex: ix,
                Timestamp: amount.Item1,
                Amount: Convert.ToUInt64(amount.Item2.Value),
                FromAccountBaseAddress: accountTransactionDetails.Sender.GetBaseAddress().ToString()
                )));
        }

        if (events.Count > 0)
        {
            var distinctBaseAddresses = events
                .Select(x => x.ToAccountBaseAddress)
                .Concat(events.Select(x => x.FromAccountBaseAddress))
                .Distinct();
            var accountIdMap = _accountLookup.GetAccountIdsFromBaseAddresses(distinctBaseAddresses);

            toInsert = events.Select(x => new AccountReleaseScheduleItem
                {
                    AccountId = accountIdMap[x.ToAccountBaseAddress] ?? throw new InvalidOperationException("Account does not exist!"),
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

    private sealed record ScheduleHolder(
        string ToAccountBaseAddress,
        long TransactionId, int ScheduleIndex,
        DateTimeOffset Timestamp, 
        UInt64 Amount,
        string FromAccountBaseAddress);

    public IEnumerable<AccountStatementEntry> GetAccountStatementEntries(
        AccountBalanceUpdate[] balanceUpdates, AccountUpdateResult[] accountUpdateResults, Block block, TransactionPair[] transactions)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountChangeCalculator), nameof(GetAccountStatementEntries));

        var distinctBaseAddresses = balanceUpdates
            .Select(x => x.AccountAddress.GetBaseAddress().ToString())
            .Distinct();
        var accountIdMap = _accountLookup.GetAccountIdsFromBaseAddresses(distinctBaseAddresses);

        var result = balanceUpdates.Select(x => new AccountStatementEntry
            {
                AccountId = accountIdMap[x.AccountAddress.GetBaseAddress().ToString()] ?? throw new InvalidOperationException("Account not found!"),
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
        if (update is not AccountBalanceUpdateWithTransaction withTransaction)
        {
            return null;
        }

        var transaction = transactions.Single(x => x.Source.TransactionHash == withTransaction.TransactionHash!);
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
