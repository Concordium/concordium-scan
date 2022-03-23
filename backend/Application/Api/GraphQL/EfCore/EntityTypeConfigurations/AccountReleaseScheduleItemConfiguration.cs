using Application.Api.GraphQL.EfCore.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class AccountReleaseScheduleItemConfiguration : IEntityTypeConfiguration<AccountReleaseScheduleItem>
{
    public void Configure(EntityTypeBuilder<AccountReleaseScheduleItem> builder)
    {
        builder.ToTable("graphql_account_release_schedule");

        builder.HasKey(x => new { x.AccountId, x.Timestamp, x.TransactionId, x.Index });
        builder.Property(x => x.AccountId).HasColumnName("account_id");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
        builder.Property(x => x.Index).HasColumnName("schedule_index");
        builder.Property(x => x.Timestamp).HasColumnName("timestamp").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.Amount).HasColumnName("amount");
        builder.Property(x => x.FromAccountId).HasColumnName("from_account_id");
    }
}