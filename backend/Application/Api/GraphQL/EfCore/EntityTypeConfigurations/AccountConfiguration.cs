using Application.Api.GraphQL.EfCore.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class AccountConfiguration : IEntityTypeConfiguration<Account>
{
    public void Configure(EntityTypeBuilder<Account> builder)
    {
        builder.ToTable("graphql_accounts");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(x => x.BaseAddress).HasColumnName("base_address").HasConversion<AccountAddressConverter>();
        builder.Property(x => x.CanonicalAddress).HasColumnName("canonical_address");
        builder.Property(x => x.Amount).HasColumnName("ccd_amount");
        builder.Property(x => x.TransactionCount).HasColumnName("transaction_count");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at").HasConversion<DateTimeOffsetToTimestampConverter>();

    }
}