using System.Threading.Tasks;
using System.Transactions;
using Application.Database;
using ConcordiumSdk.NodeApi.Types;
using HotChocolate.Subscriptions;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class DataUpdateController
{
    private readonly ITopicEventSender _sender;
    private readonly BlockWriter _blockWriter;
    private readonly IdentityProviderWriter _identityProviderWriter;
    private readonly AccountReleaseScheduleWriter _accountReleaseScheduleWriter;
    private readonly TransactionWriter _transactionWriter;
    private readonly AccountWriter _accountWriter;

    public DataUpdateController(IDbContextFactory<GraphQlDbContext> dbContextFactory, DatabaseSettings databaseSettings,
        ITopicEventSender sender)
    {
        _sender = sender;
        _blockWriter = new BlockWriter(dbContextFactory);
        _identityProviderWriter = new IdentityProviderWriter(dbContextFactory);
        _transactionWriter = new TransactionWriter(dbContextFactory);
        _accountWriter = new AccountWriter(dbContextFactory);
        _accountReleaseScheduleWriter = new AccountReleaseScheduleWriter(databaseSettings);
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
        await _identityProviderWriter.AddOrUpdateIdentityProviders(blockSummary.TransactionSummaries);
        
        var block = await _blockWriter.AddBlock(blockInfo, blockSummary, rewardStatus);

        var transactions = await _transactionWriter.AddTransactions(blockSummary, block.Id);

        await _accountWriter.AddAccounts(createdAccounts, blockInfo.BlockSlotTime);
        await _accountWriter.AddAccountTransactionRelations(transactions);
        await _accountReleaseScheduleWriter.AddAccountReleaseScheduleItems(transactions);

        await _blockWriter.CalculateAndUpdateTotalAmountLockedInSchedules(block.Id, block.BlockSlotTime);

        // TODO: Subscriptions should be sent AFTER db-tx is committed!
        await _sender.SendAsync(nameof(Subscription.BlockAdded), block);
    }
}