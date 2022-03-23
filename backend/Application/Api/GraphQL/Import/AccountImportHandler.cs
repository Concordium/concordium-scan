using System.Threading.Tasks;
using Application.Api.GraphQL.EfCore;
using Application.Import;
using ConcordiumSdk.NodeApi.Types;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.Import;

public class AccountImportHandler
{
    private readonly IAccountLookup _accountLookup;
    private readonly AccountChangeCalculator _changeCalculator;
    private readonly AccountWriter _writer;

    public AccountImportHandler(IDbContextFactory<GraphQlDbContext> dbContextFactory, IAccountLookup accountLookup)
    {
        _accountLookup = accountLookup;
        _changeCalculator = new AccountChangeCalculator(_accountLookup);
        _writer = new AccountWriter(dbContextFactory);
    }

    public async Task AddNewAccounts(AccountInfo[] createdAccounts, DateTimeOffset blockSlotTime)
    {
        var accounts = _changeCalculator.GetAccounts(createdAccounts, blockSlotTime).ToArray();
        await _writer.InsertAccounts(accounts);

        foreach (var account in accounts)
            _accountLookup.AddToCache(account.BaseAddress.AsString, account.Id);
    }

    public async Task HandleAccountUpdates(BlockDataPayload payload, TransactionPair[] transactions, Block block)
    {
        var transactionRelations = await _changeCalculator.GetAccountTransactionRelations(transactions);
        await _writer.InsertAccountTransactionRelation(transactionRelations);
        
        var balanceUpdates = payload.BlockSummary.GetAccountBalanceUpdates().ToArray();
        var accountUpdates = await _changeCalculator.GetAggregatedAccountUpdates(balanceUpdates, transactionRelations);
        await _writer.UpdateAccounts(accountUpdates);

        var statementEntries = await _changeCalculator.GetAccountStatementEntries(balanceUpdates, block.BlockSlotTime);
        await _writer.InsertAccountStatementEntries(statementEntries);

        var releaseScheduleItems = await _changeCalculator.GetAccountReleaseScheduleItems(transactions);
        await _writer.InsertAccountReleaseScheduleItems(releaseScheduleItems);
    }
}

public record AccountUpdate(long AccountId, long AmountAdjustment, int TransactionsAdded);
