using Application.Api.GraphQL.Blocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BlockRelatedFinalizationSummaryPartyConfiguration : IEntityTypeConfiguration<BlockRelated<FinalizationSummaryParty>>
{
    public void Configure(EntityTypeBuilder<BlockRelated<FinalizationSummaryParty>> builder)
    {
        builder.ToTable("graphql_finalization_summary_finalizers");

        builder.HasKey(x => new { x.BlockId, x.Index });
        builder.Property(x => x.BlockId).HasColumnName("block_id");
        builder.Property(x => x.Index).HasColumnName("index");
        builder.OwnsOne(x => x.Entity, builder =>
        {
            builder.Property(x => x.BakerId).HasColumnName("baker_id");
            builder.Property(x => x.Weight).HasColumnName("weight");
            builder.Property(x => x.Signed).HasColumnName("signed");
        });
    }
}