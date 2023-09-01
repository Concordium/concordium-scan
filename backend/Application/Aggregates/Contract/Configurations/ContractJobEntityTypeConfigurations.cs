using Application.Aggregates.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public sealed class ContractJobEntityTypeConfigurations : IEntityTypeConfiguration<ContractJob>
{
    public void Configure(EntityTypeBuilder<ContractJob> builder)
    {
        builder.ToTable("graphql_contract_jobs");
        builder.HasKey(x => x.Job);
        builder.Property(x => x.Job)
            .HasColumnName("job");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}