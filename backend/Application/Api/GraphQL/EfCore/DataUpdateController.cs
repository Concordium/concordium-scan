using System.Threading.Tasks;
using System.Transactions;
using ConcordiumSdk.NodeApi.Types;
using Dapper;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class DataUpdateController
{
    private readonly IDbContextFactory<GraphQlDbContext> _dcContextFactory;

    private readonly ITopicEventSender _sender;
    private readonly BlockWriter _blockWriter;
    private readonly IdentityProviderWriter _identityProviderWriter;
    private readonly AccountReleaseScheduleWriter _accountReleaseScheduleWriter;
    private readonly TransactionWriter _transactionWriter;

    public DataUpdateController(IDbContextFactory<GraphQlDbContext> dcContextFactory, ITopicEventSender sender,
        BlockWriter blockWriter, IdentityProviderWriter identityProviderWriter, AccountReleaseScheduleWriter accountReleaseScheduleWriter)
    {
        _dcContextFactory = dcContextFactory;
        _sender = sender;
        _blockWriter = blockWriter;
        _identityProviderWriter = identityProviderWriter;
        _accountReleaseScheduleWriter = accountReleaseScheduleWriter;
        _transactionWriter = new TransactionWriter(_dcContextFactory);
    }

    public async Task GenesisBlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary,
        AccountInfo[] createdAccounts, RewardStatus rewardStatus, IdentityProviderInfo[] identityProviders)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

        await HandleGenesisOnlyWrites(identityProviders);
        await HandleCommonWrites(blockInfo, blockSummary, rewardStatus, createdAccounts);
        
        scope.Complete();
    }

    public async Task BlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary, AccountInfo[] createdAccounts,
        RewardStatus rewardStatus)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

        await HandleCommonWrites(blockInfo, blockSummary, rewardStatus, createdAccounts);

        scope.Complete();
    }

    private async Task HandleGenesisOnlyWrites(IdentityProviderInfo[] identityProviders)
    {
        await _identityProviderWriter.AddGenesisIdentityProviders(identityProviders);
    }

    private async Task HandleCommonWrites(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus,
        AccountInfo[] createdAccounts)
    {
        // TODO: Handle updates later - consider also implementing a replay feature to support migrations?

        await _identityProviderWriter.AddOrUpdateIdentityProviders(blockSummary.TransactionSummaries);
        var block = await _blockWriter.AddBlock(blockInfo, blockSummary, rewardStatus);
        
        var transactions = await _transactionWriter.AddTransactions(blockSummary, block.Id);

        await using var context = await _dcContextFactory.CreateDbContextAsync();

        var accounts = createdAccounts.Select(x => new Account
        {
            Address = x.AccountAddress.AsString,
            CreatedAt = blockInfo.BlockSlotTime
        }).ToArray();
        context.Accounts.AddRange(accounts);
        await context.SaveChangesAsync();

        var accountTransactions = transactions
            .Select(x => new
            {
                TransactionId = x.Target.Id,
                DistinctAccountAddresses = FindAccountAddresses(x.Source, x.Target).Distinct()
            })
            .SelectMany(x => x.DistinctAccountAddresses
                .Select(accountAddress => new
                {
                    AccountAddress = accountAddress.AsString, 
                    x.TransactionId
                }))
            .ToArray();

        if (accountTransactions.Length > 0)
        {
            var connection = context.Database.GetDbConnection();

            // Inserted via dapper to inline lookup of account id from account address directly in insert
            await connection.ExecuteAsync(@"
                insert into graphql_account_transactions (account_id, transaction_id)
                select id, @TransactionId from graphql_accounts where address = @AccountAddress;", accountTransactions);
        }

        await _accountReleaseScheduleWriter.AddAccountReleaseScheduleItems(transactions);
        await _blockWriter.CalculateAndUpdateTotalAmountLockedInSchedules(block.Id, block.BlockSlotTime);
        
        // TODO: Subscriptions should be sent AFTER db-tx is committed!
        await _sender.SendAsync(nameof(Subscription.BlockAdded), block);
    }

    private IEnumerable<ConcordiumSdk.Types.AccountAddress> FindAccountAddresses(TransactionSummary source, Transaction mapped)
    {
        if (source.Sender != null) yield return source.Sender;
        foreach (var address in source.Result.GetAccountAddresses())
            yield return address;
    }
}