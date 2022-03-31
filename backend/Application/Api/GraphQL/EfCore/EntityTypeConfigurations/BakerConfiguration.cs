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
        builder.Property(x => x.Status).HasColumnName("status");
        builder.Property(x => x.PendingBakerChange).HasColumnName("pending_change").HasColumnType("json").HasConversion<PendingBakerChangeToJsonConverter>();
    }
}