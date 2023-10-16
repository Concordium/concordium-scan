using Application.Aggregates.Contract.Entities;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public sealed class ModuleReferenceEventEntityTypeConfigurations : IEntityTypeConfiguration<ModuleReferenceEvent>
{
    public void Configure(EntityTypeBuilder<ModuleReferenceEvent> builder)
    {
        builder.ToTable("graphql_module_reference_events");
        builder.HasKey(x => new
        {
            x.ModuleReference
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
        builder.Property(x => x.Sender)
            .HasColumnName("sender")
            .HasConversion<AccountAddressConverter>(); 
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.ModuleSource)
            .HasColumnName("module_source");
        builder.Property(x => x.Schema)
            .HasColumnName("schema");
        builder.Property(x => x.SchemaVersion)
            .HasColumnName("schema_version");
        builder.Property(x => x.BlockSlotTime)
            .HasColumnName("block_slot_time");        
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}
