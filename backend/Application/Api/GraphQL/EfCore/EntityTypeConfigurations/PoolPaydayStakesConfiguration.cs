using Application.Api.GraphQL.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PoolPaydayStakesConfiguration : IEntityTypeConfiguration<PoolPaydayStakes>
{
    public void Configure(EntityTypeBuilder<PoolPaydayStakes> builder)
    {
        builder.ToTable("graphql_pool_payday_stakes");
        builder.HasKey(x => new { x.PayoutBlockId, BakerId = x.PoolId});

        builder.Property(x => x.PayoutBlockId).HasColumnName("payout_block_id").ValueGeneratedNever();
        builder.Property(x => x.PoolId).HasColumnName("pool_id").ValueGeneratedNever();
        builder.Property(x => x.BakerStake).HasColumnName("baker_stake");
        builder.Property(x => x.DelegatedStake).HasColumnName("delegated_stake");
    }
}