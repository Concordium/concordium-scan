using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class IdentityProviderConfiguration : IEntityTypeConfiguration<IdentityProvider>
{
    public void Configure(EntityTypeBuilder<IdentityProvider> builder)
    {
        builder.ToTable("graphql_identity_providers");

        builder.HasKey(x => new { x.IpIdentity });
        builder.Property(x => x.IpIdentity).HasColumnName("ip_identity").ValueGeneratedNever();
        builder.Property(x => x.Name).HasColumnName("name");
        builder.Property(x => x.Url).HasColumnName("url");
        builder.Property(x => x.Description).HasColumnName("description");
    }
}