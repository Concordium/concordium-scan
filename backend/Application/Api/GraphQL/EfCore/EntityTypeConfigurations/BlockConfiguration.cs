using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BlockConfiguration : IEntityTypeConfiguration<Block>
{
    public void Configure(EntityTypeBuilder<Block> builder)
    {
        builder.ToTable("graphql_blocks");

        builder.HasKey(x => x.Id);
        builder.Property(b => b.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(b => b.BlockHash).HasColumnName("block_hash");
        builder.Property(b => b.BlockHeight).HasColumnName("block_height");
        builder.Property(b => b.BlockSlotTime).HasColumnName("block_slot_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(b => b.BakerId).HasColumnName("baker_id");
        builder.Property(b => b.Finalized).HasColumnName("finalized");
        builder.Property(b => b.TransactionCount).HasColumnName("transaction_count");
        builder.OwnsOne(block => block.SpecialEventsOld, specialEventsBuilder =>
        {
            specialEventsBuilder.WithOwner(x => x.Owner);
            specialEventsBuilder.OwnsOne(x => x.Mint, builder =>
            {
                builder.Property(m => m.BakingReward).HasColumnName("mint_baking_reward");
                builder.Property(m => m.FinalizationReward).HasColumnName("mint_finalization_reward");
                builder.Property(m => m.PlatformDevelopmentCharge).HasColumnName("mint_platform_development_charge");
                builder.Property(m => m.FoundationAccountAddress).HasColumnName("mint_foundation_account").HasConversion<AccountAddressConverter>();
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
                builder.Property(x => x.BakerAccountAddress).HasColumnName("block_reward_baker_address").HasConversion<AccountAddressConverter>();
                builder.Property(x => x.FoundationAccountAddress).HasColumnName("block_reward_foundation_account").HasConversion<AccountAddressConverter>();
            });
            specialEventsBuilder.OwnsOne(x => x.BakingRewards, builder =>
            {
                builder.WithOwner(x => x.Owner);
                builder.Property(f => f.Remainder).HasColumnName("baking_reward_remainder");
            });
        });
        builder.OwnsOne(block => block.FinalizationSummary, builder =>
        {
            builder.WithOwner(x => x.Owner);
            builder.Property(x => x.FinalizedBlockHash).HasColumnName("finalization_data_block_pointer");
            builder.Property(x => x.FinalizationIndex).HasColumnName("finalization_data_index");
            builder.Property(x => x.FinalizationDelay).HasColumnName("finalization_data_delay");
        });
        builder.OwnsOne(block => block.BalanceStatistics, builder =>
        {
            builder.Property(x => x.TotalAmount).HasColumnName("bal_stats_total_amount");
            builder.Property(x => x.TotalAmountReleased).HasColumnName("bal_stats_total_amount_released");
            builder.Property(x => x.TotalAmountEncrypted).HasColumnName("bal_stats_total_amount_encrypted");
            builder.Property(x => x.TotalAmountLockedInReleaseSchedules).HasColumnName("bal_stats_total_amount_locked_in_schedules");
            builder.Property(x => x.TotalAmountStaked).HasColumnName("bal_stats_total_amount_staked");
            builder.Property(x => x.BakingRewardAccount).HasColumnName("bal_stats_baking_reward_account");
            builder.Property(x => x.FinalizationRewardAccount).HasColumnName("bal_stats_finalization_reward_account");
            builder.Property(x => x.GasAccount).HasColumnName("bal_stats_gas_account");
        });
        builder.OwnsOne(block => block.BlockStatistics, builder =>
        {
            builder.Property(x => x.BlockTime).HasColumnName("block_stats_block_time_secs");
            builder.Property(x => x.FinalizationTime).HasColumnName("block_stats_finalization_time_secs");
        });
        builder.Property(x => x.ChainParametersId).HasColumnName("chain_parameters_id");
    }
}