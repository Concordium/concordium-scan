using Application.Api.GraphQL.Modules;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations
{
    /// <summary>
    /// Configuration of CIS token persistence in Database
    /// </summary>
    public class ContractModuleSchemaConfiguration : IEntityTypeConfiguration<ContractModuleSchema>
    {
        public void Configure(EntityTypeBuilder<ContractModuleSchema> builder)
        {
            builder.ToTable("graphql_contract_module_schema");

            builder.Property(t => t.Id).HasColumnName("id");
            builder.HasKey(t => t.Id);
            builder.Property(t => t.ModuleRef).HasColumnName("module_ref");
            builder.Property(t => t.SchemaHex).HasColumnName("schema_hex");
            builder.Property(t => t.SchemaName).HasColumnName("schema_name");
        }
    }
}
