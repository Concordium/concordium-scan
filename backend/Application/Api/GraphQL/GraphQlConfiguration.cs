using System.Numerics;
using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.ChainParametersGraphql;
using Application.Api.GraphQL.Extensions.ScalarTypes;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Metrics;
using Application.Api.GraphQL.Network;
using Application.Api.GraphQL.Pagination;
using Application.Api.GraphQL.PassiveDelegations;
using Application.Api.GraphQL.Payday;
using Application.Api.GraphQL.Search;
using Application.Api.GraphQL.Transactions;
using Application.Api.GraphQL.Versions;
using Concordium.Sdk.Types;
using HotChocolate;
using HotChocolate.Execution.Configuration;
using HotChocolate.Types;
using HotChocolate.Types.Pagination;
using Microsoft.Extensions.DependencyInjection;

namespace Application.Api.GraphQL;

public sealed class TransactionTypeType : EnumType<TransactionType>
{
    protected override void Configure(IEnumTypeDescriptor<TransactionType> descriptor)
    {
        // Change enum names from gRPC v2 to align with gRPC v1 to avoid breaking schema changes.
        descriptor.Name("AccountTransactionType");

        // Change enum value names from those in gRPC v2 to avoid breaking schema changes.
        descriptor.Value(TransactionType.InitContract)
            .Name("INITIALIZE_SMART_CONTRACT_INSTANCE");
        descriptor.Value(TransactionType.Update)
            .Name("UPDATE_SMART_CONTRACT_INSTANCE");
        descriptor.Value(TransactionType.Transfer)
            .Name("SIMPLE_TRANSFER");
        descriptor.Value(TransactionType.EncryptedAmountTransfer)
            .Name("ENCRYPTED_TRANSFER");
        descriptor.Value(TransactionType.TransferWithMemo)
            .Name("SIMPLE_TRANSFER_WITH_MEMO");
        descriptor.Value(TransactionType.EncryptedAmountTransferWithMemo)
            .Name("ENCRYPTED_TRANSFER_WITH_MEMO");
        descriptor.Value(TransactionType.TransferWithScheduleAndMemo)
            .Name("TRANSFER_WITH_SCHEDULE_WITH_MEMO");
    }
}

public sealed class CredentialTypeType : EnumType<CredentialType>
{
    protected override void Configure(IEnumTypeDescriptor<CredentialType> descriptor)
    {
        // Change enum names from gRPC v2 to align with gRPC v1 to avoid breaking schema changes.
        descriptor.Name("CredentialDeploymentTransactionType");
    }
}

public sealed class UpdateTypeType : EnumType<UpdateType> {
    protected override void Configure(IEnumTypeDescriptor<UpdateType> descriptor)
    {
        // Change enum names from gRPC v2 to align with gRPC v1 to avoid breaking schema changes.
        descriptor.Name("UpdateTransactionType");
        
        // Change enum value names from those in gRPC v2 to avoid breaking schema changes.
        descriptor.Value(UpdateType.ProtocolUpdate)
            .Name("UPDATE_PROTOCOL");
        descriptor.Value(UpdateType.ElectionDifficultyUpdate)
            .Name("UPDATE_ELECTION_DIFFICULTY");
        descriptor.Value(UpdateType.EuroPerEnergyUpdate)
            .Name("UPDATE_EURO_PER_ENERGY");
        descriptor.Value(UpdateType.MicroCcdPerEuroUpdate)
            .Name("UPDATE_MICRO_GTU_PER_EURO");
        descriptor.Value(UpdateType.FoundationAccountUpdate)
            .Name("UPDATE_FOUNDATION_ACCOUNT");
        descriptor.Value(UpdateType.MintDistributionUpdate)
            .Name("UPDATE_MINT_DISTRIBUTION");
        descriptor.Value(UpdateType.TransactionFeeDistributionUpdate)
            .Name("UPDATE_TRANSACTION_FEE_DISTRIBUTION");
        descriptor.Value(UpdateType.GasRewardsUpdate)
            .Name("UPDATE_GAS_REWARDS");
        descriptor.Value(UpdateType.BakerStakeThresholdUpdate)
            .Name("UPDATE_BAKER_STAKE_THRESHOLD");
        descriptor.Value(UpdateType.AddAnonymityRevokerUpdate)
            .Name("UPDATE_ADD_ANONYMITY_REVOKER");
        descriptor.Value(UpdateType.AddIdentityProviderUpdate)
            .Name("UPDATE_ADD_IDENTITY_PROVIDER");
        descriptor.Value(UpdateType.RootUpdate)
            .Name("UPDATE_ROOT_KEYS");
        descriptor.Value(UpdateType.Level1Update)
            .Name("UPDATE_LEVEL1_KEYS");
        descriptor.Value(UpdateType.Level2Update)
            .Name("UPDATE_LEVEL2_KEYS");
        descriptor.Value(UpdateType.PoolParametersCpv1Update)
            .Name("UPDATE_POOL_PARAMETERS");
        descriptor.Value(UpdateType.CooldownParametersCpv1Update)
            .Name("UPDATE_COOLDOWN_PARAMETERS");
        descriptor.Value(UpdateType.TimeParametersCpv1Update)
            .Name("UPDATE_TIME_PARAMETERS");
    }
}

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
            .AddCursorPagingProvider<PaydayPoolRewardByDescendingIndexCursorPagingProvider>(providerName:"payday_pool_reward_by_descending_index")
            .AddCursorPagingProvider<AccountRewardByDescendingIndexCursorPagingProvider>(providerName:"account_reward_by_descending_index")
            .AddCursorPagingProvider<AccountTokensDescendingPagingProvider>(providerName:"account_token_descending")
            .AddCursorPagingProvider<BakerTransactionRelationByDescendingIndexCursorPagingProvider>(providerName:"baker_transaction_relation_by_descending_index");
    }

    private static void ConfigureSchema(ISchemaBuilder builder)
    {
        builder.AddType<TransactionTypeType>();
        builder.AddType<CredentialTypeType>();
        builder.AddType<UpdateTypeType>();
        
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
            .AddType<NetworkQuery>();
        
        builder.AddSubscriptionType<Subscription>();
        
        builder.BindClrType<ulong, UnsignedLongType>();
        builder.BindClrType<BigInteger, BigIntegerScalarType>();

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
