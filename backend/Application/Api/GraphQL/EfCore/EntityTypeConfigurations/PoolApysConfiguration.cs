using Application.Api.GraphQL.Bakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PoolApysConfiguration : IEntityTypeConfiguration<PoolApys>
{
    public void Configure(EntityTypeBuilder<PoolApys> builder)
    {
        builder.ToView("graphql_pool_mean_apys")
            .HasKey(x => x.PoolId);
        
        builder.Property(x => x.PoolId).HasColumnName("pool_id");
        builder.OwnsOne(x => x.Apy7Days, childBuilder =>
        {
            childBuilder.Property(x => x.TotalApy).HasColumnName("total_apy_geom_mean_7_days");
            childBuilder.Property(x => x.BakerApy).HasColumnName("baker_apy_geom_mean_mean_7_days");
            childBuilder.Property(x => x.DelegatorsApy).HasColumnName("delegators_apy_geom_mean_mean_7_days");
        });
        builder.OwnsOne(x => x.Apy30Days, childBuilder =>
        {
            childBuilder.Property(x => x.TotalApy).HasColumnName("total_apy_geom_mean_30_days");
            childBuilder.Property(x => x.BakerApy).HasColumnName("baker_apy_geom_mean_30_days");
            childBuilder.Property(x => x.DelegatorsApy).HasColumnName("delegators_apy_geom_mean_30_days");
        });
    }
}