using Application.Api.GraphQL.Contracts;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class ContractConfiguration : IEntityTypeConfiguration<Contract>
{
    public void Configure(EntityTypeBuilder<Contract> builder)
    {
        builder.ToView("graphql_contracts_view");
        builder.Property(s => s.Id).HasColumnName("id");
        builder.Property(s => s.ContractAddress).HasColumnName("contract_address").HasConversion<ContractAddressConverter>();
        builder.Property(s => s.ModuleRef).HasColumnName("module_ref");
        builder.Property(s => s.Balance).HasColumnName("balance");
        builder.Property(s => s.FirstTransactionId).HasColumnName("first_transaction_id");
        builder.Property(s => s.LastTransactionId).HasColumnName("last_transaction_id");
        builder.Property(s => s.TransactionsCount).HasColumnName("transactions_count");
        builder.Property(s => s.Owner).HasColumnName("owner").HasConversion<AccountAddressConverter>();
        builder.Property(s => s.CreatedTime).HasColumnName("created_time").HasConversion<DateTimeOffsetToTimestampConverter>();
    }
}
