using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Payday;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PaydaySummaryConfiguration : IEntityTypeConfiguration<PaydaySummary>
{
    public void Configure(EntityTypeBuilder<PaydaySummary> builder)
    {
        builder.ToTable("graphql_payday_summaries");
        builder.HasKey(x => x.BlockId);

        builder.Property(x => x.BlockId).HasColumnName("block_id").ValueGeneratedNever();
        builder.Property(x => x.PaydayTime).HasColumnName("payday_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.PaydayDurationSeconds).HasColumnName("payday_duration_seconds");
    }
}