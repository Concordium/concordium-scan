using Application.Aggregates.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public class ContractSnapshotEntityTypeConfigurations : IEntityTypeConfiguration<ContractSnapshot>
{
    public void Configure(EntityTypeBuilder<ContractSnapshot> builder)
    {
        builder.ToTable("graphql_contract_snapshot");
        builder.HasKey(x => new
        {
            x.BlockHeight,
            x.ContractAddressIndex,
            x.ContractAddressSubIndex
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.ContractAddressIndex)
            .HasColumnName("contract_address_index");
        builder.Property(x => x.ContractAddressSubIndex)
            .HasColumnName("contract_address_subindex");
        
        builder.Property(x => x.ContractName)
            .HasColumnName("contract_name");
        builder.Property(x => x.ModuleReference)
            .HasColumnName("module_reference");
        builder.Property(x => x.Amount)
            .HasColumnName("amount");
        
        builder.Property(x => x.Source)
            .HasColumnName("source");
        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}
