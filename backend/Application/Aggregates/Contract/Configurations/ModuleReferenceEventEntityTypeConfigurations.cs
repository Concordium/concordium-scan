using Application.Aggregates.Contract.Entities;
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
            x.BlockHeight,
            x.TransactionIndex,
            x.EventIndex,
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
        builder.Property(x => x.Source)
            .HasColumnName("source");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");        
    }
}