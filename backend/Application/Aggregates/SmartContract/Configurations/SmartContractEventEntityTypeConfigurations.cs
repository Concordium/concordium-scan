using Application.Aggregates.SmartContract.Entities;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.SmartContract.Configurations;

public sealed class SmartContractEventEntityTypeConfigurations : IEntityTypeConfiguration<SmartContractEvent>
{
    public void Configure(EntityTypeBuilder<SmartContractEvent> builder)
    {
        builder.ToTable("graphql_smart_contract_events");
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
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}