using Application.Api.GraphQL.Bakers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class BakerTransactionRelationConfiguration : IEntityTypeConfiguration<BakerTransactionRelation>
{
    public void Configure(EntityTypeBuilder<BakerTransactionRelation> builder)
    {
        builder.ToTable("graphql_baker_transactions");
        
        builder.HasKey(x => new { x.BakerId, x.Index });
        builder.Property(x => x.BakerId).HasColumnName("baker_id").ValueGeneratedNever();
        builder.Property(x => x.Index).HasColumnName("index").ValueGeneratedOnAdd();
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
    }
}