using System.Threading.Tasks;
using System.Transactions;
using Application.Common;
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
    private readonly TransactionWriter _transactionWriter;
    private readonly AccountWriter _accountWriter;
    private readonly MetricsWriter _metricsWriter;

    private readonly MemoryCacheManager _cacheManager;
    private readonly IMemoryCachedValue<DateTimeOffset> _previousBlockSlotTime;
    private readonly IMemoryCachedValue<long> _cumulativeTransactionCountState;
    private readonly IMemoryCachedValue<long> _cumulativeAccountsCreatedState;

    public DataUpdateController(IDbContextFactory<GraphQlDbContext> dbContextFactory, DatabaseSettings dbSettings, ITopicEventSender sender)
    {
        _sender = sender;
        _blockWriter = new BlockWriter(dbContextFactory);
        _identityProviderWriter = new IdentityProviderWriter(dbContextFactory);
        _transactionWriter = new TransactionWriter(dbContextFactory);
        _accountWriter = new AccountWriter(dbContextFactory);
        _metricsWriter = new MetricsWriter(dbSettings);
        
        _cacheManager = new();
        _previousBlockSlotTime = _cacheManager.CreateCachedValue<DateTimeOffset>();
        _cumulativeTransactionCountState = _cacheManager.CreateCachedValue<long>();
        _cumulativeAccountsCreatedState = _cacheManager.CreateCachedValue<long>();
    }

    public async Task GenesisBlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary,
        AccountInfo[] createdAccounts, RewardStatus rewardStatus, IdentityProviderInfo[] identityProviders)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

        await HandleGenesisOnlyWrites(identityProviders);
        var block = await HandleCommonWrites(blockInfo, blockSummary, rewardStatus, createdAccounts);
        
        scope.Complete();
        _cacheManager.CommitEnqueuedUpdates();
        
        await _sender.SendAsync(nameof(Subscription.BlockAdded), block);
    }

    public async Task BlockDataReceived(BlockInfo blockInfo, BlockSummary blockSummary, AccountInfo[] createdAccounts,
        RewardStatus rewardStatus)
    {
        using var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.ReadCommitted }, TransactionScopeAsyncFlowOption.Enabled);

        var block = await HandleCommonWrites(blockInfo, blockSummary, rewardStatus, createdAccounts);

        scope.Complete();
        _cacheManager.CommitEnqueuedUpdates();

        await _sender.SendAsync(nameof(Subscription.BlockAdded), block);
    }

    private async Task HandleGenesisOnlyWrites(IdentityProviderInfo[] identityProviders)
    {
        await _identityProviderWriter.AddGenesisIdentityProviders(identityProviders);
    }

    private async Task<Block> HandleCommonWrites(BlockInfo blockInfo, BlockSummary blockSummary, RewardStatus rewardStatus,
        AccountInfo[] createdAccounts)
    {
        await _identityProviderWriter.AddOrUpdateIdentityProviders(blockSummary.TransactionSummaries);
        
        var block = await _blockWriter.AddBlock(blockInfo, blockSummary, rewardStatus);
        var transactions = await _transactionWriter.AddTransactions(blockSummary, block.Id);

        await _accountWriter.AddAccounts(createdAccounts, blockInfo.BlockSlotTime);

        await _accountWriter.AddAccountTransactionRelations(transactions);
        await _accountWriter.AddAccountReleaseScheduleItems(transactions);

        await _blockWriter.CalculateAndUpdateTotalAmountLockedInSchedules(block.Id, block.BlockSlotTime);

        await _metricsWriter.AddBlockMetrics(blockInfo, rewardStatus, _previousBlockSlotTime);
        await _metricsWriter.AddTransactionMetrics(blockInfo, blockSummary, _cumulativeTransactionCountState);
        await _metricsWriter.AddAccountsMetrics(blockInfo, createdAccounts, _cumulativeAccountsCreatedState);

        return block;
    }
}