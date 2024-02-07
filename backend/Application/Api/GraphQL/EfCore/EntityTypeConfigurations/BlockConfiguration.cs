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
        builder.OwnsOne(block => block.BalanceStatistics, builder =>
        {
            builder.Property(x => x.TotalAmount).HasColumnName("bal_stats_total_amount");
            builder.Property(x => x.TotalAmountReleased).HasColumnName("bal_stats_total_amount_released");
            builder.Property(x => x.TotalAmountUnlocked).HasColumnName("bal_stats_total_amount_unlocked");
            builder.Property(x => x.TotalAmountEncrypted).HasColumnName("bal_stats_total_amount_encrypted");
            builder.Property(x => x.TotalAmountLockedInReleaseSchedules).HasColumnName("bal_stats_total_amount_locked_in_schedules");
            builder.Property(x => x.TotalAmountStaked).HasColumnName("bal_stats_total_amount_staked");
            builder.Property(x => x.TotalAmountStakedByBakers).HasColumnName("bal_stats_total_amount_staked_by_bakers");
            builder.Property(x => x.TotalAmountStakedByDelegation).HasColumnName("bal_stats_total_amount_staked_by_delegation");
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
