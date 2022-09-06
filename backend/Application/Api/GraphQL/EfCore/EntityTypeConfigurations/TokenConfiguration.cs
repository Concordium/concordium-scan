using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Tokens;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations
{
    /// <summary>
    /// Configuration of CIS token persistence in Database
    /// </summary>
    public class TokenConfiguration : IEntityTypeConfiguration<Token>
    {
        public void Configure(EntityTypeBuilder<Token> builder)
        {
            builder.ToTable("graphql_tokens");

            builder.Property(t => t.ContractIndex).HasColumnName("contract_index");
            builder.Property(t => t.ContractSubIndex).HasColumnName("contract_sub_index");
            builder.Property(t => t.TokenId).HasColumnName("token_id");
            builder.Property(t => t.MetadataUrl).HasColumnName("metadata_url");
            builder.Property(t => t.TotalSupply).HasColumnName("total_supply");
            
            builder.HasKey(t => new { t.ContractIndex, t.ContractSubIndex, t.TokenId });
        }
    }
}