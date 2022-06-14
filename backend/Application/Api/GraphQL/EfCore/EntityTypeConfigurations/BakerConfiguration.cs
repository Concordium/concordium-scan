using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BakerConfiguration : 
    IEntityTypeConfiguration<Baker>,
    IEntityTypeConfiguration<BakerStatisticsRow>
{
    public void Configure(EntityTypeBuilder<Baker> builder)
    {
        builder.ToTable("graphql_bakers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.OwnsOne(x => x.ActiveState, activeBuilder =>
        {
            activeBuilder.WithOwner(x => x.Owner);
            activeBuilder.Property(x => x.StakedAmount).HasColumnName("active_staked_amount");
            activeBuilder.Property(x => x.RestakeEarnings).HasColumnName("active_restake_earnings");
            activeBuilder.Property(x => x.PendingChange).HasColumnName("active_pending_change").HasColumnType("json").HasConversion<PendingBakerChangeToJsonConverter>();
            
            activeBuilder.OwnsOne(x => x.Pool, poolBuilder =>
            {
                poolBuilder.WithOwner(x => x.Owner);

                poolBuilder.Property(x => x.OpenStatus).HasColumnName("active_pool_open_status");
                poolBuilder.Property(x => x.MetadataUrl).HasColumnName("active_pool_metadata_url");
                poolBuilder.OwnsOne(x => x.CommissionRates, commissionRatesBuilder =>
                {
                    commissionRatesBuilder.Property(x => x.TransactionCommission).HasColumnName("active_pool_transaction_commission");
                    commissionRatesBuilder.Property(x => x.FinalizationCommission).HasColumnName("active_pool_finalization_commission");
                    commissionRatesBuilder.Property(x => x.BakingCommission).HasColumnName("active_pool_baking_commission");
                });
                poolBuilder.Property(x => x.DelegatedStake).HasColumnName("active_pool_delegated_stake");
                poolBuilder.Property(x => x.DelegatedStakeCap).HasColumnName("active_pool_delegated_stake_cap");
                poolBuilder.Property(x => x.TotalStake).HasColumnName("active_pool_total_stake");
                poolBuilder.Property(x => x.DelegatorCount).HasColumnName("active_pool_delegator_count");
                poolBuilder.OwnsOne(x => x.PaydayStatus, paydayStatusBuilder =>
                {
                    paydayStatusBuilder.Property(x => x.BakerStake).HasColumnName("active_pool_payday_status_baker_stake");
                    paydayStatusBuilder.Property(x => x.DelegatedStake).HasColumnName("active_pool_payday_status_delegated_stake");
                    paydayStatusBuilder.Property(x => x.EffectiveStake).HasColumnName("active_pool_payday_status_effective_stake");
                    paydayStatusBuilder.Property(x => x.LotteryPower).HasColumnName("active_pool_payday_status_lottery_power");
                });
            });

        });
        builder.OwnsOne(x => x.RemovedState, removedBuilder =>
        {
            removedBuilder.Property(x => x.RemovedAt).HasColumnName("removed_timestamp").HasConversion<DateTimeOffsetToTimestampConverter>();
        });

        builder.HasOne(x => x.Statistics);
        builder.Navigation(x => x.Statistics).AutoInclude();

        builder.Ignore(x => x.BakerId);
        builder.Ignore(x => x.State);
    }
    
    public void Configure(EntityTypeBuilder<BakerStatisticsRow> builder)
    {
        builder.ToView("graphql_baker_statistics")
            .HasKey(x => x.BakerId);
        
        builder.Property(x => x.BakerId).HasColumnName("baker_id");
        builder.Property(x => x.PoolTotalStakePercentage).HasColumnName("active_pool_total_stake_percentage");
        builder.Property(x => x.PoolRankByTotalStake).HasColumnName("active_baker_pool_rank_by_total_stake");
        builder.Property(x => x.ActiveBakerPoolCount).HasColumnName("active_baker_pool_count");
    }
}
