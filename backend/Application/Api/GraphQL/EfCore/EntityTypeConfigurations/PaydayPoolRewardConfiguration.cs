using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PaydayPoolRewardConfiguration : IEntityTypeConfiguration<PaydayPoolReward>
{
    public void Configure(EntityTypeBuilder<PaydayPoolReward> builder)
    {
        builder.ToTable("metrics_payday_pool_rewards");
        builder.HasKey(x => x.Index);

        builder.Property(x => x.Index).HasColumnName("index").ValueGeneratedOnAdd();
        builder.Property(x => x.Timestamp).HasColumnName("time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.Pool).HasColumnName("pool_id").HasConversion<PoolRewardTargetToLongConverter>();
        builder.Property(x => x.TransactionFeesTotalAmount).HasColumnName("transaction_fees_total_amount");
        builder.Property(x => x.TransactionFeesBakerAmount).HasColumnName("transaction_fees_baker_amount");
        builder.Property(x => x.TransactionFeesDelegatorsAmount).HasColumnName("transaction_fees_delegator_amount");
        builder.Property(x => x.BakerRewardTotalAmount).HasColumnName("baker_reward_total_amount");
        builder.Property(x => x.BakerRewardBakerAmount).HasColumnName("baker_reward_baker_amount");
        builder.Property(x => x.BakerRewardDelegatorsAmount).HasColumnName("baker_reward_delegator_amount");
        builder.Property(x => x.FinalizationRewardTotalAmount).HasColumnName("finalization_reward_total_amount");
        builder.Property(x => x.FinalizationRewardBakerAmount).HasColumnName("finalization_reward_baker_amount");
        builder.Property(x => x.FinalizationRewardDelegatorsAmount).HasColumnName("finalization_reward_delegator_amount");
        builder.Property(x => x.SumTotalAmount).HasColumnName("sum_total_amount");
        builder.Property(x => x.SumBakerAmount).HasColumnName("sum_baker_amount");
        builder.Property(x => x.SumDelegatorsAmount).HasColumnName("sum_delegator_amount");
        builder.Property(x => x.PaydayDurationSeconds).HasColumnName("payday_duration_seconds");
        builder.Property(x => x.TotalApy).HasColumnName("total_apy");
        builder.Property(x => x.BakerApy).HasColumnName("baker_apy");
        builder.Property(x => x.DelegatorsApy).HasColumnName("delegators_apy");
        builder.Property(x => x.BlockId).HasColumnName("block_id");
    }
}