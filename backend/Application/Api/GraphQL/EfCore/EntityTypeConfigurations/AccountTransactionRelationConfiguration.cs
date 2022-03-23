using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class AccountTransactionRelationConfiguration : IEntityTypeConfiguration<AccountTransactionRelation>
{
    public void Configure(EntityTypeBuilder<AccountTransactionRelation> builder)
    {
        builder.ToTable("graphql_account_transactions");
        
        builder.HasKey(x => new { x.AccountId, x.Index });
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.Index).HasColumnName("index");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
    }
}