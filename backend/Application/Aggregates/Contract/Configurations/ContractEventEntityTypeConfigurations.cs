using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public sealed class ContractEventEntityTypeConfigurations : IEntityTypeConfiguration<ContractEvent>
{
    public void Configure(EntityTypeBuilder<ContractEvent> builder)
    {
        builder.ToTable("graphql_contract_events");
        builder.HasKey(x => new
        {
            x.BlockHeight, 
            x.TransactionIndex,
            x.EventIndex,
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
        builder.Property(x => x.Event)
            .HasColumnName("event")
            .HasColumnType("json")
            .HasConversion<TransactionResultEventToJsonConverter>();
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.BlockSlotTime)
            .HasColumnName("block_slot_time");        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}