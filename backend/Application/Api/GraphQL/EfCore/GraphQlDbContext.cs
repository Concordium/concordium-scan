using Application.Api.GraphQL.EfCore.Converters;
using Microsoft.EntityFrameworkCore;

namespace Application.Api.GraphQL.EfCore;

public class GraphQlDbContext : DbContext
{
    public DbSet<ImportState> ImportState { get; private set; }
    public DbSet<Block> Blocks { get; private set; }
    public DbSet<BlockRelated<FinalizationReward>> FinalizationRewards { get; private set; }
    public DbSet<BlockRelated<BakingReward>> BakingRewards { get; private set; }
    public DbSet<BlockRelated<FinalizationSummaryParty>> FinalizationSummaryFinalizers { get; private set; }
    public DbSet<Transaction> Transactions { get; private set; }
    public DbSet<Account> Accounts { get; private set; }
    public DbSet<TransactionRelated<TransactionResultEvent>> TransactionResultEvents { get; private set; }
    public DbSet<AccountTransactionRelation> AccountTransactionRelations { get; private set; }
    public DbSet<IdentityProvider> IdentityProviders { get; private set; }

    public GraphQlDbContext(DbContextOptions options) : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var importStateBuilder = modelBuilder.Entity<ImportState>()
            .ToTable("graphql_import_state");

        importStateBuilder.HasKey(x => x.Id);
        importStateBuilder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        importStateBuilder.Property(x => x.GenesisBlockHash).HasColumnName("genesis_block_hash");
        importStateBuilder.Property(x => x.MaxImportedBlockHeight).HasColumnName("max_imported_block_height");
        importStateBuilder.Property(x => x.CumulativeAccountsCreated).HasColumnName("cumulative_accounts_created");
        importStateBuilder.Property(x => x.CumulativeTransactionCount).HasColumnName("cumulative_transaction_count");
        importStateBuilder.Property(x => x.LastBlockSlotTime).HasColumnName("last_block_slot_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        importStateBuilder.Property(x => x.MaxBlockHeightWithUpdatedFinalizationTime).HasColumnName("max_block_height_with_updated_finalization_time");
        
        var blockBuilder = modelBuilder.Entity<Block>()
            .ToTable("graphql_blocks");

        blockBuilder.HasKey(x => x.Id);
        blockBuilder.Property(b => b.Id).HasColumnName("id").ValueGeneratedOnAdd();
        blockBuilder.Property(b => b.BlockHash).HasColumnName("block_hash");
        blockBuilder.Property(b => b.BlockHeight).HasColumnName("block_height");
        blockBuilder.Property(b => b.BlockSlotTime).HasColumnName("block_slot_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        blockBuilder.Property(b => b.BakerId).HasColumnName("baker_id");
        blockBuilder.Property(b => b.Finalized).HasColumnName("finalized");
        blockBuilder.Property(b => b.TransactionCount).HasColumnName("transaction_count");
        blockBuilder.OwnsOne(block => block.SpecialEvents, specialEventsBuilder =>
        {
            specialEventsBuilder.WithOwner(x => x.Owner);
            specialEventsBuilder.OwnsOne(x => x.Mint, builder =>
            {
                builder.Property(m => m.BakingReward).HasColumnName("mint_baking_reward");
                builder.Property(m => m.FinalizationReward).HasColumnName("mint_finalization_reward");
                builder.Property(m => m.PlatformDevelopmentCharge).HasColumnName("mint_platform_development_charge");
                builder.Property(m => m.FoundationAccount).HasColumnName("mint_foundation_account");
            });
            specialEventsBuilder.OwnsOne(x => x.FinalizationRewards, builder =>
            {
                builder.WithOwner(x => x.Owner);
                builder.Property(f => f.Remainder).HasColumnName("finalization_reward_remainder");
            });
            specialEventsBuilder.OwnsOne(x => x.BlockRewards, builder =>
            {
                builder.Property(x => x.TransactionFees).HasColumnName("block_reward_transaction_fees");
                builder.Property(x => x.OldGasAccount).HasColumnName("block_reward_old_gas_account");
                builder.Property(x => x.NewGasAccount).HasColumnName("block_reward_new_gas_account");
                builder.Property(x => x.BakerReward).HasColumnName("block_reward_baker_reward");
                builder.Property(x => x.FoundationCharge).HasColumnName("block_reward_foundation_charge");
                builder.Property(x => x.BakerAccountAddress).HasColumnName("block_reward_baker_address");
                builder.Property(x => x.FoundationAccountAddress).HasColumnName("block_reward_foundation_account");
            });
            specialEventsBuilder.OwnsOne(x => x.BakingRewards, builder =>
            {
                builder.WithOwner(x => x.Owner);
                builder.Property(f => f.Remainder).HasColumnName("baking_reward_remainder");
            });
        });
        blockBuilder.OwnsOne(block => block.FinalizationSummary, builder =>
        {
            builder.WithOwner(x => x.Owner);
            builder.Property(x => x.FinalizedBlockHash).HasColumnName("finalization_data_block_pointer");
            builder.Property(x => x.FinalizationIndex).HasColumnName("finalization_data_index");
            builder.Property(x => x.FinalizationDelay).HasColumnName("finalization_data_delay");
        });
        blockBuilder.OwnsOne(block => block.BalanceStatistics, builder =>
        {
            builder.Property(x => x.TotalAmount).HasColumnName("bal_stats_total_amount");
            builder.Property(x => x.TotalEncryptedAmount).HasColumnName("bal_stats_total_encrypted_amount");
            builder.Property(x => x.TotalAmountLockedInReleaseSchedules).HasColumnName("bal_stats_total_amount_locked_in_schedules");
            builder.Property(x => x.BakingRewardAccount).HasColumnName("bal_stats_baking_reward_account");
            builder.Property(x => x.FinalizationRewardAccount).HasColumnName("bal_stats_finalization_reward_account");
            builder.Property(x => x.GasAccount).HasColumnName("bal_stats_gas_account");
        });
        blockBuilder.OwnsOne(block => block.BlockStatistics, builder =>
        {
            builder.Property(x => x.BlockTime).HasColumnName("block_stats_block_time_secs");
            builder.Property(x => x.FinalizationTime).HasColumnName("block_stats_finalization_time_secs");
        });

        var finalizationRewardsBuilder = modelBuilder.Entity<BlockRelated<FinalizationReward>>()
            .ToTable("graphql_finalization_rewards");

        finalizationRewardsBuilder.HasKey(x => new { x.BlockId, x.Index });
        finalizationRewardsBuilder.Property(x => x.BlockId).HasColumnName("block_id");
        finalizationRewardsBuilder.Property(x => x.Index).HasColumnName("index");
        finalizationRewardsBuilder.OwnsOne(x => x.Entity, builder =>
        {
            builder.Property(x => x.Address).HasColumnName("address");
            builder.Property(x => x.Amount).HasColumnName("amount");
        });
        
        var bakingRewardsBuilder = modelBuilder.Entity<BlockRelated<BakingReward>>()
            .ToTable("graphql_baking_rewards");

        bakingRewardsBuilder.HasKey(x => new { x.BlockId, x.Index });
        bakingRewardsBuilder.Property(x => x.BlockId).HasColumnName("block_id");
        bakingRewardsBuilder.Property(x => x.Index).HasColumnName("index");
        bakingRewardsBuilder.OwnsOne(x => x.Entity, builder =>
        {
            builder.Property(x => x.Address).HasColumnName("address");
            builder.Property(x => x.Amount).HasColumnName("amount");
        });
        
        var finalizersBuilder = modelBuilder.Entity<BlockRelated<FinalizationSummaryParty>>()
            .ToTable("graphql_finalization_summary_finalizers");

        finalizersBuilder.HasKey(x => new { x.BlockId, x.Index });
        finalizersBuilder.Property(x => x.BlockId).HasColumnName("block_id");
        finalizersBuilder.Property(x => x.Index).HasColumnName("index");
        finalizersBuilder.OwnsOne(x => x.Entity, builder =>
        {
            builder.Property(x => x.BakerId).HasColumnName("baker_id");
            builder.Property(x => x.Weight).HasColumnName("weight");
            builder.Property(x => x.Signed).HasColumnName("signed");
        });

        var transactionBuilder = modelBuilder.Entity<Transaction>()
            .ToTable("graphql_transactions");

        transactionBuilder.HasKey(x => x.Id);
        transactionBuilder.Property(x => x.Id).HasColumnName("id");
        transactionBuilder.Property(b => b.BlockId).HasColumnName("block_id");
        transactionBuilder.Property(b => b.TransactionIndex).HasColumnName("index");
        transactionBuilder.Property(b => b.TransactionHash).HasColumnName("transaction_hash");
        transactionBuilder.Property(b => b.SenderAccountAddress).HasColumnName("sender");
        transactionBuilder.Property(b => b.CcdCost).HasColumnName("micro_ccd_cost");
        transactionBuilder.Property(b => b.EnergyCost).HasColumnName("energy_cost");
        transactionBuilder.Property(b => b.TransactionType).HasColumnName("transaction_type").HasConversion<TransactionTypeToStringConverter>();
        transactionBuilder.Property(b => b.RejectReason).HasColumnName("reject_reason").HasColumnType("json").HasConversion<TransactionRejectReasonToJsonConverter>();  
        transactionBuilder.Ignore(b => b.Result); // TODO: Map to nullable reject reason string (json). If null it is a success, otherwise a failure and reject reason can be deserialized from this string data.

        var transactionEventBuilder = modelBuilder.Entity<TransactionRelated<TransactionResultEvent>>()
            .ToTable("graphql_transaction_events");

        transactionEventBuilder.HasKey(x => new { x.TransactionId, x.Index });
        transactionEventBuilder.Property(x => x.TransactionId).HasColumnName("transaction_id");
        transactionEventBuilder.Property(x => x.Index).HasColumnName("index");
        transactionEventBuilder.Property(x => x.Entity).HasColumnName("event").HasColumnType("json").HasConversion<TransactionResultEventToJsonConverter>();

        var accountBuilder = modelBuilder.Entity<Account>()
            .ToTable("graphql_accounts");

        accountBuilder.HasKey(x => x.Id);
        accountBuilder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        accountBuilder.Property(x => x.BaseAddress).HasColumnName("base_address").HasConversion<AccountAddressConverter>();
        accountBuilder.Property(x => x.CanonicalAddress).HasColumnName("canonical_address");
        accountBuilder.Property(x => x.CreatedAt).HasColumnName("created_at").HasConversion<DateTimeOffsetToTimestampConverter>();

        var accountTransactionRelationBuilder = modelBuilder.Entity<AccountTransactionRelation>()
            .ToTable("graphql_account_transactions");
        
        accountTransactionRelationBuilder.HasKey(x => new { x.AccountId, x.Index });
        accountTransactionRelationBuilder.Property(x => x.AccountId).HasColumnName("account_id");
        accountTransactionRelationBuilder.Property(x => x.Index).HasColumnName("index");
        accountTransactionRelationBuilder.Property(x => x.TransactionId).HasColumnName("transaction_id");

        var identityProviderBuilder = modelBuilder.Entity<IdentityProvider>()
            .ToTable("graphql_identity_providers");

        identityProviderBuilder.HasKey(x => new { x.IpIdentity });
        identityProviderBuilder.Property(x => x.IpIdentity).HasColumnName("ip_identity").ValueGeneratedNever();
        identityProviderBuilder.Property(x => x.Name).HasColumnName("name");
        identityProviderBuilder.Property(x => x.Url).HasColumnName("url");
        identityProviderBuilder.Property(x => x.Description).HasColumnName("description");
    }
}

