using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.EfCore;

public class AccountChangeCalculator
{
    private readonly IAccountLookup _accountLookup;

    public AccountChangeCalculator(IAccountLookup accountLookup)
    {
        _accountLookup = accountLookup;
    }

    public async Task<IEnumerable<AccountWriter.AccountUpdate>> CreateAggregatedAccountUpdates(IEnumerable<AccountBalanceUpdate> balanceUpdates, AccountTransactionRelation[] transactionRelations)
    {
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
        var accountIdMap = await _accountLookup.GetAccountIdsFromBaseAddressesAsync(baseAddresses);
        
        var amountAdjustmentResults = aggregatedBalanceUpdates
            .Select(x =>
            {
                var accountId = accountIdMap[x.BaseAddress] ?? throw new InvalidOperationException("Attempt at updating account that does not exist!");
                return new AccountWriter.AccountUpdate(accountId, x.AmountAdjustment, 0);
            })
            .Where(x => x.AmountAdjustment != 0);

        var transactionResults = transactionRelations
            .GroupBy(x => x.AccountId)
            .Select(x => new { AccountId = x.Key, TransactionsAdded = x.Aggregate(0, (acc, _) => ++acc) })
            .Select(x => new AccountWriter.AccountUpdate(x.AccountId, 0, x.TransactionsAdded));

        return amountAdjustmentResults.Concat(transactionResults)
            .GroupBy(x => x.AccountId)
            .Select(group => new AccountWriter.AccountUpdate(group.Key,
                group.Aggregate(0L, (acc, item) => acc + item.AmountAdjustment),
                group.Aggregate(0, (acc, item) => acc + item.TransactionsAdded)));
    }

    public async Task<IEnumerable<AccountStatementEntry>> CreateAccountStatementEntries(AccountBalanceUpdate[] balanceUpdates, DateTimeOffset blockSlotTime)
    {
        var distinctBaseAddresses = balanceUpdates
            .Select(x => x.AccountAddress.GetBaseAddress().AsString)
            .Distinct();
        var accountIdMap = await _accountLookup.GetAccountIdsFromBaseAddressesAsync(distinctBaseAddresses);
        
        return balanceUpdates.Select(x => new AccountStatementEntry
        {
            AccountId = accountIdMap[x.AccountAddress.GetBaseAddress().AsString] ?? throw new InvalidOperationException("Account not found!"),
            Index = default, // Will be set by database
            Timestamp = blockSlotTime,
            Amount = x.AmountAdjustment,
            EntryType = Map(x.balanceUpdateType)
        });
    }

    private EntryType Map(BalanceUpdateType input)
    {
        return input switch
        {
            BalanceUpdateType.AmountDecrypted => EntryType.AmountDecrypted,
            BalanceUpdateType.AmountEncrypted => EntryType.AmountEncrypted,
            BalanceUpdateType.BakingReward => EntryType.BakingReward,
            BalanceUpdateType.BlockReward => EntryType.BlockReward,
            BalanceUpdateType.FinalizationReward => EntryType.FinalizationReward,
            BalanceUpdateType.MintReward => EntryType.MintReward,
            BalanceUpdateType.TransactionFee => EntryType.TransactionFee,
            BalanceUpdateType.TransferIn => EntryType.TransferIn,
            BalanceUpdateType.TransferOut => EntryType.TransferOut,
            _ => throw new NotImplementedException()
        };
    }
}