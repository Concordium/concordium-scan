using Application.Api.GraphQL.Bakers;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BakerConfiguration : IEntityTypeConfiguration<Baker>
{
    public void Configure(EntityTypeBuilder<Baker> builder)
    {
        builder.ToTable("graphql_bakers");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.OwnsOne(x => x.ActiveState, activeBuilder =>
        {
            activeBuilder.Property(x => x.RestakeEarnings).HasColumnName("active_restake_earnings");
            activeBuilder.Property(x => x.PendingChange).HasColumnName("active_pending_change").HasColumnType("json").HasConversion<PendingBakerChangeToJsonConverter>();
        });
        builder.OwnsOne(x => x.RemovedState, removedBuilder =>
        {
            removedBuilder.Property(x => x.RemovedAt).HasColumnName("removed_timestamp").HasConversion<DateTimeOffsetToTimestampConverter>();
        });
        
        builder.Ignore(x => x.BakerId);
        builder.Ignore(x => x.State);
    }
}