using Application.Aggregates.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations;

public sealed class TokenTransactionConfiguration : IEntityTypeConfiguration<TokenEvent>
{
    public void Configure(EntityTypeBuilder<TokenEvent> builder)
    {
        builder.ToTable("graphql_token_events");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id");
        builder.Property(x => x.ContractIndex).HasColumnName("contract_address_index").HasColumnType("bigint");
        builder.Property(x => x.ContractSubIndex).HasColumnName("contract_address_subindex").HasColumnType("bigint");
        builder.Property(x => x.TokenId).HasColumnName("token_id").HasColumnType("text");
        builder.Property(x => x.Event).HasColumnName("event").HasColumnType("json")
            .HasConversion<CisEventToJsonConverter>();
    }
}
