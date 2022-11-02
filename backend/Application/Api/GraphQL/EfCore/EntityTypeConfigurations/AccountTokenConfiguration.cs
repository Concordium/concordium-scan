using Application.Api.GraphQL.Accounts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations
{
    /// <summary>
    /// Configuration of Account Token Persistence in Database.
    /// </summary>
    public class AccountTokenConfiguration : IEntityTypeConfiguration<AccountToken>
    {
        public void Configure(EntityTypeBuilder<AccountToken> builder)
        {
            builder.ToTable("graphql_account_tokens");
            builder.HasKey(t => new { t.ContractIndex, t.ContractSubIndex, t.TokenId, t.AccountId });
            builder.Property(t => t.AccountId).HasColumnName("account_id");
            builder.Property(t => t.Balance).HasColumnName("balance");
            builder.Property(t => t.TokenId).HasColumnName("token_id");
            builder.Property(t => t.ContractIndex).HasColumnName("contract_index");
            builder.Property(t => t.ContractSubIndex).HasColumnName("contract_sub_index");
            builder.Property(x => x.Index).HasColumnName("index").ValueGeneratedOnAdd();

            builder.HasOne(t => t.Token)
                .WithMany()
                .HasForeignKey(t => new { t.ContractIndex, t.ContractSubIndex, t.TokenId })
                .HasPrincipalKey(t => new { t.ContractIndex, t.ContractSubIndex, t.TokenId });
        }
    }
}