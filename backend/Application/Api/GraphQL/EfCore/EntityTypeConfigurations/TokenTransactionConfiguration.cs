using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Application.Api.GraphQL.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public sealed class TokenTransactionConfiguration : IEntityTypeConfiguration<TokenEvents>
{
    public void Configure(EntityTypeBuilder<TokenEvents> builder)
    {
        builder.ToTable("graphql_token_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ContractIndex).HasColumnName("contract_address_index").HasColumnType("bigint");
        builder.Property(x => x.ContractSubIndex).HasColumnName("contract_address_subindex").HasColumnType("bigint");
        builder.Property(x => x.TransactionId).HasColumnName("transaction_id").HasColumnType("bigint");
        builder.Property(x => x.TokenId).HasColumnName("token_id").HasColumnType<string>("text");
        builder.Property(x => x.Event).HasColumnName("event").HasColumnType("json")
            .HasConversion<CisEventDataToJsonConverter>();
    }
}
