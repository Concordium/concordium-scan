using Application.Aggregates.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public sealed class ContractReadHeightEntityTypeConfigurations : IEntityTypeConfiguration<
    ContractReadHeight>
{
    public void Configure(EntityTypeBuilder<ContractReadHeight> builder)
    {
        builder.ToTable("graphql_contract_read_heights");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}