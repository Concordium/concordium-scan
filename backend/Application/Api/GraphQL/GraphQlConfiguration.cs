using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Metrics;
using Application.Api.GraphQL.Pagination;
using Application.Api.GraphQL.Search;
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
            .AddCursorPagingProvider<AccountTransactionRelationByDescendingIndexCursorPagingProvider>(providerName:"account_transaction_relation_by_descending_index");
    }

    private static void ConfigureSchema(ISchemaBuilder builder)
    {
        builder.AddQueryType<Query>()
            .AddType<AccountsQuery>()
            .AddType<SearchQuery>()
            .AddType<MetricsQuery>();
        
        builder.AddSubscriptionType<Subscription>();
        
        builder.BindClrType<ulong, UnsignedLongType>();

        // Bind all concrete types of GraphQL unions and interfaces
        AddAllTypesDerivedFrom<TransactionResult>(builder);
        AddAllTypesDerivedFrom<TransactionTypeUnion>(builder);
        AddAllTypesDerivedFrom<Address>(builder);
        AddAllTypesDerivedFrom<TransactionResultEvent>(builder);
        AddAllTypesDerivedFrom<TransactionRejectReason>(builder);
    }

    private static void AddAllTypesDerivedFrom<T>(ISchemaBuilder builder)
    {
        var result = typeof(T).Assembly.GetExportedTypes()
            .Where(type => type.IsAssignableTo(typeof(T)) && type != typeof(T))
            .ToArray();
        
        builder.AddTypes(result);
    }
}
