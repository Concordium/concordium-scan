using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public sealed class ContractEntityTypeConfigurations : IEntityTypeConfiguration<Entities.Contract>
{
    public void Configure(EntityTypeBuilder<Entities.Contract> builder)
    {
        builder.ToTable("graphql_contracts");
        builder.HasKey(x => new
        {
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
        builder.Property(x => x.ContractAddressIndex)
            .HasColumnName("contract_address_index");
        builder.Property(x => x.ContractAddressSubIndex)
            .HasColumnName("contract_address_sub_index");
        builder.Property(x => x.Creator)
            .HasColumnName("creator")
            .HasConversion<AccountAddressConverter>();
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.BlockSlotTime)
            .HasColumnName("block_slot_time");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");

        builder
            .HasMany<ContractEvent>(sm => sm.ContractEvents)
            .WithOne()
            .HasForeignKey(sme => new { sme.ContractAddressIndex, sme.ContractAddressSubIndex });
        
        builder
            .HasMany<ModuleReferenceContractLinkEvent>(c => c.ModuleReferenceContractLinkEvents)
            .WithOne()
            .HasForeignKey(link => new { link.ContractAddressIndex, link.ContractAddressSubIndex });
    }
}