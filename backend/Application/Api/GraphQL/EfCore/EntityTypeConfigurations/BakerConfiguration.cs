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
        builder.Property(x => x.StakePercentage).HasColumnName("active_stake_percentage");
        builder.Property(x => x.RankByStake).HasColumnName("active_baker_rank_by_stake");
        builder.Property(x => x.ActiveBakerCount).HasColumnName("active_baker_count");
    }
}
