using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Common.Diagnostics;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class AccountImportHandler
{
    private readonly IAccountLookup _accountLookup;
    private readonly IMetrics _metrics;
    private readonly AccountChangeCalculator _changeCalculator;
    private readonly AccountWriter _writer;

    public AccountImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IAccountLookup accountLookup, IMetrics metrics)
    {
        _accountLookup = accountLookup;
        _metrics = metrics;
        _changeCalculator = new AccountChangeCalculator(_accountLookup);
        _writer = new AccountWriter(dbContextFactory, metrics);
    }

    public async Task AddNewAccounts(AccountInfo[] createdAccounts, DateTimeOffset blockSlotTime)
    {
        if (createdAccounts.Length == 0) return;
        
        var accounts = _changeCalculator.GetAccounts(createdAccounts, blockSlotTime).ToArray();
        await _writer.InsertAccounts(accounts);

        foreach (var account in accounts)
            _accountLookup.AddToCache(account.BaseAddress.AsString, account.Id);
    }

    public async Task HandleAccountUpdates(BlockDataPayload payload, TransactionPair[] transactions, Block block)
    {
        using var counter = _metrics.MeasureDuration(nameof(AccountImportHandler), nameof(HandleAccountUpdates));

        var transactionRelations = await _changeCalculator.GetAccountTransactionRelations(transactions);
        if (transactionRelations.Length > 0)
            await _writer.InsertAccountTransactionRelation(transactionRelations);
        
        var balanceUpdates = payload.BlockSummary.GetAccountBalanceUpdates().ToArray();
        var accountUpdates = await _changeCalculator.GetAggregatedAccountUpdates(balanceUpdates, transactionRelations);
        await _writer.UpdateAccounts(accountUpdates);

        var statementEntries = await _changeCalculator.GetAccountStatementEntries(balanceUpdates, block.BlockSlotTime);
        await _writer.InsertAccountStatementEntries(statementEntries);

        var releaseScheduleItems = await _changeCalculator.GetAccountReleaseScheduleItems(transactions);
        if (releaseScheduleItems.Length > 0)
            await _writer.InsertAccountReleaseScheduleItems(releaseScheduleItems);
    }
}

public record AccountUpdate(long AccountId, long AmountAdjustment, int TransactionsAdded);
