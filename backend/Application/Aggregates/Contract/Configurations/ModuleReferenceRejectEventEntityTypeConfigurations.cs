using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using AccountAddressConverter = Application.Api.GraphQL.EfCore.Converters.EfCore.AccountAddressConverter;

namespace Application.Aggregates.Contract.Configurations;

public sealed class ModuleReferenceRejectEventEntityTypeConfigurations : IEntityTypeConfiguration<ModuleReferenceRejectEvent>
{
    public void Configure(EntityTypeBuilder<ModuleReferenceRejectEvent> builder)
    {
        builder.ToTable("graphql_module_reference_reject_events");
        builder.HasKey(x => new
        {
            x.BlockHeight,
            x.TransactionIndex,
            x.ModuleReference
        });
        builder.Property(x => x.BlockHeight)
            .HasColumnName("block_height");
        builder.Property(x => x.TransactionHash)
            .HasColumnName("transaction_hash");
        builder.Property(x => x.TransactionIndex)
            .HasColumnName("transaction_index");
        builder.Property(x => x.ModuleReference)
            .HasColumnName("module_reference");
        builder.Property(x => x.Sender)
            .HasColumnName("sender")
            .HasConversion<AccountAddressConverter>(); 
        builder.Property(x => x.RejectedEvent)
            .HasColumnName("reject_event")
            .HasColumnType("json")
            .HasConversion<TransactionRejectReasonToJsonConverter>();        
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.BlockSlotTime)
            .HasColumnName("block_slot_time");        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}