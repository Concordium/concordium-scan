using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class TransactionRelatedTransactionResultEventConfiguration : IEntityTypeConfiguration<TransactionRelated<TransactionResultEvent>>
{
    public void Configure(EntityTypeBuilder<TransactionRelated<TransactionResultEvent>> builder)
    {
        builder.ToTable("graphql_transaction_events");

        builder.HasKey(x => new { x.TransactionId, x.Index });
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
        builder.Property(x => x.Index).HasColumnName("index");
        builder.Property(x => x.Entity).HasColumnName("event").HasColumnType("json").HasConversion<TransactionResultEventToJsonConverter>();
    }
}