using Application.Api.GraphQL.EfCore.Converters;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Import;
using Application.Api.GraphQL.Tokens;
using Application.Api.GraphQL.Transactions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class TokenTransactionConfiguration : IEntityTypeConfiguration<TokenTransaction>
{
    public void Configure(EntityTypeBuilder<TokenTransaction> builder)
    {
        builder.ToTable("graphql_token_transactions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ContractIndex).HasColumnName("contract_index").HasColumnType("bigint");
        builder.Property(x => x.ContractSubIndex).HasColumnName("contract_subindex").HasColumnType("bigint");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id").HasColumnType("bigint");
        builder.Property(x => x.TokenId).HasColumnName("token_id").HasColumnType<string>("text");
        builder.Property(x => x.Data).HasColumnName("data").HasColumnType("json").HasConversion<CisEventDataToJsonConverter>();
    }
}