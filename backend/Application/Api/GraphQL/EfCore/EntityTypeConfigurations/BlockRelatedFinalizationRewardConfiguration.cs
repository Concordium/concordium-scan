using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BlockRelatedFinalizationRewardConfiguration : IEntityTypeConfiguration<BlockRelated<FinalizationReward>>
{
    public void Configure(EntityTypeBuilder<BlockRelated<FinalizationReward>> builder)
    {
        builder.ToTable("graphql_finalization_rewards");

        builder.HasKey(x => new { x.BlockId, x.Index });
        builder.Property(x => x.BlockId).HasColumnName("block_id");
        builder.Property(x => x.Index).HasColumnName("index");
        builder.OwnsOne(x => x.Entity, builder =>
        {
            builder.Property(x => x.Address).HasColumnName("address").HasConversion<AccountAddressConverter>();
            builder.Property(x => x.Amount).HasColumnName("amount");
        });
    }
}