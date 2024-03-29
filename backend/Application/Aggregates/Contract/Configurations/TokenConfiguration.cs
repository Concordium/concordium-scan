using Application.Aggregates.Contract.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Aggregates.Contract.Configurations
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
            builder.Property(t => t.TokenAddress).HasColumnName("token_address");
            builder.Property(t => t.TotalSupply).HasColumnName("total_supply");
            
            builder.HasKey(t => new { t.ContractIndex, t.ContractSubIndex, t.TokenId });
        }
    }
}
