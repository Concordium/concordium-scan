using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Contracts;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class ContractTransactionRelationConfiguration : IEntityTypeConfiguration<ContractTransactionRelation>
{
    public void Configure(EntityTypeBuilder<ContractTransactionRelation> builder)
    {
        builder.ToView("graphql_contract_transactions_view");
        builder.HasNoKey();
        builder.Property(x => x.ContractAddress).HasColumnName("contract_address").HasConversion<ContractAddressConverter>();
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id");
    }
}
