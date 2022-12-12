using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Payday;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PaydayStatusConfiguration : IEntityTypeConfiguration<PaydayStatus>
{
    public void Configure(EntityTypeBuilder<PaydayStatus> builder)
    {
        builder.ToTable("graphql_payday_status");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.NextPaydayTime).HasColumnName("next_payday_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.PaydayStartTime).HasColumnName("payday_start_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.ProtocolVersion).HasColumnName("protocol_version");
    }
}