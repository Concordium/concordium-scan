using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class TransactionConfiguration : IEntityTypeConfiguration<Transaction>
{
    public void Configure(EntityTypeBuilder<Transaction> builder)
    {
        builder.ToTable("graphql_transactions");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(b => b.BlockId).HasColumnName("block_id");
        builder.Property(b => b.TransactionIndex).HasColumnName("index");
        builder.Property(b => b.TransactionHash).HasColumnName("transaction_hash");
        builder.Property(b => b.SenderAccountAddress).HasColumnName("sender").HasConversion<AccountAddressConverter>();
        builder.Property(b => b.CcdCost).HasColumnName("micro_ccd_cost");
        builder.Property(b => b.EnergyCost).HasColumnName("energy_cost");
        builder.Property(b => b.TransactionType).HasColumnName("transaction_type").HasConversion<TransactionTypeToStringConverter>();
        builder.Property(b => b.RejectReason).HasColumnName("reject_reason").HasColumnType("json").HasConversion<TransactionRejectReasonToJsonConverter>();  
        builder.Ignore(b => b.Result); // Mapped dynamically from either RejectReason or from tx-events
    }
}