using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BlockRelatedBakingRewardConfiguration : IEntityTypeConfiguration<BlockRelated<BakingReward>>
{
    public void Configure(EntityTypeBuilder<BlockRelated<BakingReward>> builder)
    {
        builder.ToTable("graphql_baking_rewards");

        builder.HasKey(x => new { x.BlockId, x.Index });
        builder.Property(x => x.BlockId).HasColumnName("block_id");
        builder.Property(x => x.Index).HasColumnName("index");
        builder.OwnsOne(x => x.Entity, builder =>
        {
            builder.Property(x => x.Address).HasColumnName("address");
            builder.Property(x => x.Amount).HasColumnName("amount");
        });
    }
}