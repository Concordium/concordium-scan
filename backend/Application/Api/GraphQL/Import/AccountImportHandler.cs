using System.Threading.Tasks;
using Application.Api.GraphQL.Blocks;
using Application.Common.Diagnostics;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;

namespace Application.Api.GraphQL.Import;

public class AccountImportHandler
{
    private readonly IAccountLookup _accountLookup;
    private readonly IMetrics _metrics;
    private readonly AccountChangeCalculator _changeCalculator;
    private readonly AccountWriter _writer;

    public AccountImportHandler(IAccountLookup accountLookup, IMetrics metrics, AccountWriter accountWriter)
    {
        _accountLookup = accountLookup;
        _metrics = metrics;
        _changeCalculator = new AccountChangeCalculator(_accountLookup);
        _writer = accountWriter;
    }

    public async Task AddNewAccounts(AccountInfo[] createdAccounts, DateTimeOffset blockSlotTime)
    {
        if (createdAccounts.Length == 0) return;
        
        var accounts = _changeCalculator.GetAccounts(createdAccounts, blockSlotTime).ToArray();
        await _writer.InsertAccounts(accounts);

        foreach (var account in accounts)
            _accountLookup.AddToCache(account.BaseAddress.AsString, account.Id);
    }

    public void HandleAccountUpdates(BlockDataPayload payload, TransactionPair[] transactions, Block block)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountImportHandler), nameof(HandleAccountUpdates));

        var transactionRelations = _changeCalculator.GetAccountTransactionRelations(transactions);
        if (transactionRelations.Length > 0)
            _writer.InsertAccountTransactionRelation(transactionRelations);
        
        var balanceUpdates = payload.BlockSummary.GetAccountBalanceUpdates().ToArray();
        var accountUpdates = _changeCalculator.GetAggregatedAccountUpdates(balanceUpdates, transactionRelations);
        var updateResults = _writer.UpdateAccounts(accountUpdates);

        var statementEntries = _changeCalculator.GetAccountStatementEntries(balanceUpdates, updateResults, block, transactions);
        _writer.InsertAccountStatementEntries(statementEntries);

        var releaseScheduleItems = _changeCalculator.GetAccountReleaseScheduleItems(transactions);
        if (releaseScheduleItems.Length > 0)
            _writer.InsertAccountReleaseScheduleItems(releaseScheduleItems);
    }
}
