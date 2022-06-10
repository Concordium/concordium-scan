using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.Metrics;
using Application.Api.GraphQL.Network;
using Application.Api.GraphQL.Pagination;
using Application.Api.GraphQL.PassiveDelegations;
using Application.Api.GraphQL.Payday;
using Application.Api.GraphQL.Search;
using Application.Api.GraphQL.Transactions;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Api.GraphQL;

public static class GraphQlConfiguration
{
    public static IRequestExecutorBuilder Configure(this IRequestExecutorBuilder builder)
    {
        return builder.ConfigureSchema(ConfigureSchema)
            .AddInMemorySubscriptions()
            .AddCursorPagingProvider<QueryableCursorPagingProvider>(defaultProvider:true)
            .AddCursorPagingProvider<BlockByDescendingIdCursorPagingProvider>(providerName:"block_by_descending_id")
            .AddCursorPagingProvider<TransactionByDescendingIdCursorPagingProvider>(providerName:"transaction_by_descending_id")
            .AddCursorPagingProvider<AccountTransactionRelationByDescendingIndexCursorPagingProvider>(providerName:"account_transaction_relation_by_descending_index")
            .AddCursorPagingProvider<AccountStatementEntryByDescendingIndexCursorPagingProvider>(providerName:"account_statement_entry_by_descending_index")
            .AddCursorPagingProvider<PoolRewardByDescendingIndexCursorPagingProvider>(providerName:"pool_reward_by_descending_index")
            .AddCursorPagingProvider<PaydayPoolRewardByDescendingIndexCursorPagingProvider>(providerName:"payday_pool_reward_by_descending_index")
            .AddCursorPagingProvider<AccountRewardByDescendingIndexCursorPagingProvider>(providerName:"account_reward_by_descending_index")
            .AddCursorPagingProvider<BakerTransactionRelationByDescendingIndexCursorPagingProvider>(providerName:"baker_transaction_relation_by_descending_index");
    }

    private static void ConfigureSchema(ISchemaBuilder builder)
    {
        builder.AddGlobalObjectIdentification(false);
        
        builder.AddQueryType<Query>()
            .AddType<BlocksQuery>()
            .AddType<TransactionsQuery>()
            .AddType<AccountsQuery>()
            .AddType<BakersQuery>()
            .AddType<SearchQuery>()
            .AddType<AccountsMetricsQuery>()
            .AddType<BlockMetricsQuery>()
            .AddType<TransactionMetricsQuery>()
            .AddType<BakerMetricsQuery>()
            .AddType<RewardMetricsQuery>()
            .AddType<PoolRewardMetricsQuery>()
            .AddType<PassiveDelegationQuery>()
            .AddType<PaydayQuery>()
            .AddType<NetworkQuery>();
        
        builder.AddSubscriptionType<Subscription>();
        
        builder.BindClrType<ulong, UnsignedLongType>();

        // Bind all concrete types of GraphQL unions and interfaces
        AddAllTypesDerivedFrom<ChainParameters>(builder);
        AddAllTypesDerivedFrom<SpecialEvent>(builder);
        AddAllTypesDerivedFrom<TransactionResult>(builder);
        AddAllTypesDerivedFrom<TransactionTypeUnion>(builder);
        AddAllTypesDerivedFrom<Address>(builder);
        AddAllTypesDerivedFrom<TransactionResultEvent>(builder);
        AddAllTypesDerivedFrom<ChainUpdatePayload>(builder);
        AddAllTypesDerivedFrom<TransactionRejectReason>(builder);
        AddAllTypesDerivedFrom<IBlockOrTransactionUnion>(builder);
        AddAllTypesDerivedFrom<BakerState>(builder);
        AddAllTypesDerivedFrom<PendingBakerChange>(builder);
        AddAllTypesDerivedFrom<DelegationTarget>(builder);
        AddAllTypesDerivedFrom<PendingDelegationChange>(builder);
        AddAllTypesDerivedFrom<PoolRewardTarget>(builder);
    }

    private static void AddAllTypesDerivedFrom<T>(ISchemaBuilder builder)
    {
        var result = typeof(T).Assembly.GetExportedTypes()
            .Where(type => type.IsAssignableTo(typeof(T)) && type != typeof(T))
            .ToArray();
        
        builder.AddTypes(result);
    }
}
