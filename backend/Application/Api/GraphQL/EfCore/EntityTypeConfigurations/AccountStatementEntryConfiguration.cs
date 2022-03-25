using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class AccountStatementEntryConfiguration : IEntityTypeConfiguration<AccountStatementEntry>
{
    public void Configure(EntityTypeBuilder<AccountStatementEntry> builder)
    {
        builder.ToTable("graphql_account_statement_entries");
        
        builder.HasKey(x => new { x.AccountId, x.Index });
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.Index).HasColumnName("index").ValueGeneratedOnAdd();
        builder.Property(x => x.Timestamp).HasColumnName("time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.EntryType).HasColumnName("entry_type"); // TODO: Should probably use specific converter to avoid future problems
        builder.Property(x => x.Amount).HasColumnName("amount");
        builder.Property(x => x.BlockId).HasColumnName("block_id");
    }
}