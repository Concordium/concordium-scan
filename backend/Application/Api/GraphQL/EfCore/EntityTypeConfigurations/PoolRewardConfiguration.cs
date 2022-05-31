using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PoolRewardConfiguration
{
    public class PaydaySummaryConfiguration : IEntityTypeConfiguration<PoolReward>
    {
        public void Configure(EntityTypeBuilder<PoolReward> builder)
        {
            builder.ToTable("metrics_pool_rewards");
            builder.HasKey(x => x.Index);

            builder.Property(x => x.Index).HasColumnName("index").ValueGeneratedOnAdd();
            builder.Property(x => x.Timestamp).HasColumnName("time").HasConversion<DateTimeOffsetToTimestampConverter>();
            builder.Property(x => x.Pool).HasColumnName("pool_id").HasConversion<PoolRewardTargetToLong>();
            builder.Property(x => x.TotalAmount).HasColumnName("total_amount");
            builder.Property(x => x.BakerAmount).HasColumnName("baker_amount");
            builder.Property(x => x.DelegatorsAmount).HasColumnName("delegator_amount");
            builder.Property(x => x.RewardType).HasColumnName("reward_type");
            builder.Property(x => x.BlockId).HasColumnName("block_id");
        }
    }
}