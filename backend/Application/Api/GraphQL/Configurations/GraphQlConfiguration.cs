using System.Numerics;
using Application.Aggregates.Contract.Extensions;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.ChainParametersGraphql;
using Application.Api.GraphQL.EfCore;
using Application.Api.GraphQL.Extensions.ScalarTypes;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Import.EventLogs;
using Application.Api.GraphQL.Metrics;
using Application.Api.GraphQL.Network;
using Application.Api.GraphQL.Pagination;
using Application.Api.GraphQL.PassiveDelegations;
using Application.Api.GraphQL.Payday;
using Application.Api.GraphQL.Search;
using Application.Api.GraphQL.Tokens;
using Application.Api.GraphQL.Transactions;
using Application.Api.GraphQL.Versions;
using Application.Observability;
using HotChocolate;
using HotChocolate.Data;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Api.GraphQL.Configurations;

public static class GraphQlConfiguration
{
    public static IRequestExecutorBuilder Configure(this IRequestExecutorBuilder builder)
    {
        return builder.ConfigureSchema(ConfigureSchema)
            .RegisterDbContext<GraphQlDbContext>(DbContextKind.Pooled)
            .AddDiagnosticEventListener<ObservabilityExecutionDiagnosticEventListener>()
            .AddInMemorySubscriptions()
            .AddCursorPagingProvider<QueryableCursorPagingProvider>(defaultProvider: true)
            .AddCursorPagingProvider<BlockByDescendingIdCursorPagingProvider>(providerName: "block_by_descending_id")
            .AddCursorPagingProvider<TransactionByDescendingIdCursorPagingProvider>(
                providerName: "transaction_by_descending_id")
            .AddCursorPagingProvider<AccountTransactionRelationByDescendingIndexCursorPagingProvider>(
                providerName: "account_transaction_relation_by_descending_index")
            .AddCursorPagingProvider<AccountStatementEntryByDescendingIndexCursorPagingProvider>(
                providerName: "account_statement_entry_by_descending_index")
            .AddCursorPagingProvider<PaydayPoolRewardByDescendingIndexCursorPagingProvider>(
                providerName: "payday_pool_reward_by_descending_index")
            .AddCursorPagingProvider<AccountRewardByDescendingIndexCursorPagingProvider>(
                providerName: "account_reward_by_descending_index")
            .AddCursorPagingProvider<AccountTokensDescendingPagingProvider>(providerName: "account_token_descending")
            .AddCursorPagingProvider<BakerTransactionRelationByDescendingIndexCursorPagingProvider>(
                providerName: "baker_transaction_relation_by_descending_index")
            .AddContractGraphQlConfigurations();
    }

    private static void ConfigureSchema(ISchemaBuilder builder)
    {
        builder.AddGlobalObjectIdentification(false);
        
        builder.AddQueryType<Query>()
            .AddType<VersionsQuery>()
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
            .AddType<ChainParametersQuery>()
            .AddType<ImportStateQuery>()
            .AddType<NetworkQuery>()
            .AddType<TokenQuery>();

        builder.AddSubscriptionType<Subscription>();
        
        builder.BindRuntimeType<ulong, UnsignedLongType>();
        builder.BindRuntimeType<uint, UnsignedIntType>();
        builder.BindRuntimeType<BigInteger, BigIntegerScalarType>();

        builder.AddDerivedTypes();
        
        builder.AddEnumTypes();
    }

    /// <summary>
    /// Bind all concrete types of GraphQL unions and interfaces
    /// </summary>
    /// <param name="builder"></param>
    private static void AddDerivedTypes(this ISchemaBuilder builder)
    {
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
        AddAllTypesDerivedFrom<CisEvent>(builder);
    }

    private static void AddAllTypesDerivedFrom<T>(ISchemaBuilder builder)
    {
        var result = typeof(T).Assembly.GetExportedTypes()
            .Where(type => type.IsAssignableTo(typeof(T)) && type != typeof(T))
            .ToArray();
        
        builder.AddTypes(result);
    }
    
    /// <summary>
    /// Adding custom enum descriptors to GraphQL schema.
    /// </summary>
    private static void AddEnumTypes(this ISchemaBuilder builder)
    {
        builder.AddType<TransactionTypeType>();
        builder.AddType<CredentialTypeType>();
        builder.AddType<UpdateTypeType>();        
    }
}
