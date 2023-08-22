using Application.Aggregates.SmartContract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.SmartContract.Configurations;

public sealed class SmartContractJobEntityTypeConfigurations : IEntityTypeConfiguration<SmartContractJob>
{
    public void Configure(EntityTypeBuilder<SmartContractJob> builder)
    {
        builder.ToTable("graphql_smart_contract_jobs");
        builder.HasKey(x => x.Job);
        builder.Property(x => x.Job)
            .HasColumnName("job");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}