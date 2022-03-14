using System.Threading.Tasks;
using ConcordiumSdk.NodeApi.Types;
using ConcordiumSdk.Types;
using Dapper;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Api.GraphQL.EfCore;

public class AccountWriter
{
    private readonly IDbContextFactory<GraphQlDbContext> _dbContextFactory;

    public AccountWriter(IDbContextFactory<GraphQlDbContext> dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAccounts(AccountInfo[] createdAccounts, DateTimeOffset blockSlotTime)
    {
        await using var context = await _dbContextFactory.CreateDbContextAsync();

        var accounts = createdAccounts.Select(x => new Account
        {
            Id = (long)x.AccountIndex,
            CanonicalAddress = x.AccountAddress.AsString,
            BaseAddress = new AccountAddress(x.AccountAddress.GetBaseAddress().AsString),
            Amount = x.AccountAmount.MicroCcdValue,
            CreatedAt = blockSlotTime
        }).ToArray();
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();
    }

    public async Task UpdateAccountBalances(BlockSummary blockSummary)
    {
        var balanceUpdates = new List<AccountBalanceUpdate>();
        var mint =  blockSummary.SpecialEvents.OfType<MintSpecialEvent>().SingleOrDefault();
        if (mint != null)
            balanceUpdates.Add(new AccountBalanceUpdate(mint.FoundationAccount.GetBaseAddress(), (long)mint.MintPlatformDevelopmentCharge.MicroCcdValue));
        var fr =  blockSummary.SpecialEvents.OfType<FinalizationRewardsSpecialEvent>().SingleOrDefault();
        if (fr != null)
            balanceUpdates.AddRange(fr.FinalizationRewards.Select(x => new AccountBalanceUpdate(x.Address.GetBaseAddress(), (long)x.Amount.MicroCcdValue)));
        var br =  blockSummary.SpecialEvents.OfType<BlockRewardSpecialEvent>().SingleOrDefault();
        if (br != null)
        {
            balanceUpdates.Add(new AccountBalanceUpdate(br.FoundationAccount.GetBaseAddress(), (long)br.FoundationCharge.MicroCcdValue));
            balanceUpdates.Add(new AccountBalanceUpdate(br.Baker.GetBaseAddress(), (long)br.BakerReward.MicroCcdValue));
        }
        var br2 =  blockSummary.SpecialEvents.OfType<BakingRewardsSpecialEvent>().SingleOrDefault();
        if (br2 != null)
            balanceUpdates.AddRange(br2.BakerRewards.Select(x => new AccountBalanceUpdate(x.Address.GetBaseAddress(), (long)x.Amount.MicroCcdValue)));

        foreach (var transactionSummary in blockSummary.TransactionSummaries)
        {
            if (transactionSummary.Sender != null && transactionSummary.Cost > CcdAmount.Zero)
                balanceUpdates.Add(new AccountBalanceUpdate(transactionSummary.Sender.GetBaseAddress(), -1 * (long)transactionSummary.Cost.MicroCcdValue));
            if (transactionSummary.Result is TransactionSuccessResult success)
            {
                var transfers = success.Events.OfType<ConcordiumSdk.NodeApi.Types.Transferred>();
                foreach (var transfer in transfers)
                {
                    if (transfer.From is ConcordiumSdk.Types.AccountAddress fromAccountAddress)
                        balanceUpdates.Add(new AccountBalanceUpdate(fromAccountAddress.GetBaseAddress(), -1 * (long)transfer.Amount.MicroCcdValue));
                    if (transfer.To is ConcordiumSdk.Types.AccountAddress toAccountAddress)
                        balanceUpdates.Add(new AccountBalanceUpdate(toAccountAddress.GetBaseAddress(), (long)transfer.Amount.MicroCcdValue));
                }
                var transferredWithSchedules = success.Events.OfType<ConcordiumSdk.NodeApi.Types.TransferredWithSchedule>();
                foreach (var transferredWithSchedule in transferredWithSchedules)
                {
                    var totalAmount = transferredWithSchedule.Amount.Sum(x => (long)x.Amount.MicroCcdValue);
                    balanceUpdates.Add(new AccountBalanceUpdate(transferredWithSchedule.From.GetBaseAddress(), -1 * totalAmount));
                    balanceUpdates.Add(new AccountBalanceUpdate(transferredWithSchedule.To.GetBaseAddress(), totalAmount));
                }
                var amountAddedByDecryptions = success.Events.OfType<ConcordiumSdk.NodeApi.Types.AmountAddedByDecryption>();
                foreach (var amountAddedByDecryption in amountAddedByDecryptions)
                    balanceUpdates.Add(new AccountBalanceUpdate(amountAddedByDecryption.Account.GetBaseAddress(), (long)amountAddedByDecryption.Amount.MicroCcdValue));
                var encryptedSelfAmountAddeds = success.Events.OfType<ConcordiumSdk.NodeApi.Types.EncryptedSelfAmountAdded>();
                foreach (var encryptedSelfAmountAdded in encryptedSelfAmountAddeds)
                    balanceUpdates.Add(new AccountBalanceUpdate(encryptedSelfAmountAdded.Account.GetBaseAddress(), -1 * (long)encryptedSelfAmountAdded.Amount.MicroCcdValue));
                var contractsInitializeds = success.Events.OfType<ConcordiumSdk.NodeApi.Types.ContractInitialized>();
                foreach (var contractsInitialized in contractsInitializeds)
                    balanceUpdates.Add(new AccountBalanceUpdate(transactionSummary.Sender!.GetBaseAddress(), -1 * (long)contractsInitialized.Amount.MicroCcdValue));
                var contractsUpdateds = success.Events.OfType<ConcordiumSdk.NodeApi.Types.Updated>();
                foreach (var contractsUpdated in contractsUpdateds)
                    if (contractsUpdated.Instigator is ConcordiumSdk.Types.AccountAddress accountAddress)
                        balanceUpdates.Add(new AccountBalanceUpdate(accountAddress.GetBaseAddress(), -1 * (long)contractsUpdated.Amount.MicroCcdValue));
            }
        }
        // TODO: Added "RETURNING base_address, ccd_amount" to be able to write metrics for account balances!
        var sql = @"UPDATE graphql_accounts SET ccd_amount = ccd_amount + @AmountAdjustment WHERE base_address = @BaseAddress";

        await using var context = await _dbContextFactory.CreateDbContextAsync();
        var connection = context.Database.GetDbConnection();
        
        await connection.OpenAsync();
        
        var batch = connection.CreateBatch();
        foreach (var balanceUpdate in balanceUpdates)
        {
            var cmd = batch.CreateBatchCommand();
            cmd.CommandText = sql;
            cmd.Parameters.Add(new NpgsqlParameter<long>("AmountAdjustment", balanceUpdate.AmountAdjustment));
            cmd.Parameters.Add(new NpgsqlParameter<string>("BaseAddress", balanceUpdate.BaseAddress.AsString));
            batch.BatchCommands.Add(cmd);
        }
        await batch.ExecuteNonQueryAsync();
        await connection.CloseAsync();
    }

    public class AccountBalanceUpdate
    {
        public AccountBalanceUpdate(ConcordiumSdk.Types.AccountAddress baseAddress, long amountAdjustment)
        {
            BaseAddress = baseAddress;
            AmountAdjustment = amountAdjustment;
        }

        public ConcordiumSdk.Types.AccountAddress BaseAddress;
        public long AmountAdjustment;
    }
    
    public async Task AddAccountTransactionRelations(TransactionPair[] transactions)
    {
        var accountTransactions = transactions
            .Select(x => new
            {
                TransactionId = x.Target.Id,
                DistinctAccountBaseAddresses = FindAccountAddresses(x.Source, x.Target)
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
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            // Inserted via dapper to inline lookup of account id from account address directly in insert
            await connection.ExecuteAsync(@"
                insert into graphql_account_transactions (account_id, transaction_id)
                select id, @TransactionId from graphql_accounts where base_address = @AccountBaseAddress;", accountTransactions);
        }
    }
    
    public async Task AddAccountReleaseScheduleItems(IEnumerable<TransactionPair> transactions)
    {
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
                        Amount = Convert.ToInt64(amount.Amount.MicroCcdValue)
                    }));
            }).ToArray();

        if (result.Length > 0)
        {
            await using var context = await _dbContextFactory.CreateDbContextAsync();
            var connection = context.Database.GetDbConnection();

            await connection.ExecuteAsync(@"
                insert into graphql_account_release_schedule (account_id, transaction_id, schedule_index, timestamp, amount)
                select id, @TransactionId, @ScheduleIndex, @Timestamp, @Amount from graphql_accounts where base_address = @AccountBaseAddress;",
                result);
        }
    }
    private IEnumerable<ConcordiumSdk.Types.AccountAddress> FindAccountAddresses(TransactionSummary source, Transaction mapped)
    {
        if (source.Sender != null) yield return source.Sender;
        foreach (var address in source.Result.GetAccountAddresses())
            yield return address;
    }
}