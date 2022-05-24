using Application.Api.GraphQL.PassiveDelegations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class PassiveDelegationConfiguration : IEntityTypeConfiguration<PassiveDelegation>
{
    public void Configure(EntityTypeBuilder<PassiveDelegation> builder)
    {
        builder.ToTable("graphql_passive_delegation");
        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.DelegatorCount).HasColumnName("delegator_count");
        builder.Property(x => x.DelegatedStake).HasColumnName("delegated_stake");
        builder.Property(x => x.DelegatedStakePercentage).HasColumnName("delegated_stake_percentage");
    }
}