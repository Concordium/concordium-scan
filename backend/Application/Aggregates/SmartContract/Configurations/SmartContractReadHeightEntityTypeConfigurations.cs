using Application.Aggregates.SmartContract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.SmartContract.Configurations;

public sealed class SmartContractReadHeightEntityTypeConfigurations : IEntityTypeConfiguration<
    SmartContractReadHeight>
{
    public void Configure(EntityTypeBuilder<SmartContractReadHeight> builder)
    {
        builder.ToTable("graphql_smart_contract_read_heights");
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