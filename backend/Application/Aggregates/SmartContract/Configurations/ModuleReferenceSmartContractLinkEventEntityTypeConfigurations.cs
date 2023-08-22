using Application.Aggregates.SmartContract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.SmartContract.Configurations;

public sealed class ModuleReferenceSmartContractLinkEventEntityTypeConfigurations : IEntityTypeConfiguration<ModuleReferenceSmartContractLinkEvent>
{
    public void Configure(EntityTypeBuilder<ModuleReferenceSmartContractLinkEvent> builder)
    {
        builder.ToTable("graphql_module_reference_smart_contract_link_events");
        builder.HasKey(x => new
        {
            x.BlockHeight,
            x.TransactionIndex,
            x.EventIndex,
            x.ModuleReference,
            x.ContractAddressIndex,
            x.ContractAddressSubIndex
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.TransactionHash)
            .HasColumnName("transaction_hash");
        builder.Property(x => x.TransactionIndex)
            .HasColumnName("transaction_index");
        builder.Property(x => x.EventIndex)
            .HasColumnName("event_index");
        builder.Property(x => x.ModuleReference)
            .HasColumnName("module_reference");
        builder.Property(x => x.ContractAddressIndex)
            .HasColumnName("contract_address_index");
        builder.Property(x => x.ContractAddressSubIndex)
            .HasColumnName("contract_address_sub_index");       
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}