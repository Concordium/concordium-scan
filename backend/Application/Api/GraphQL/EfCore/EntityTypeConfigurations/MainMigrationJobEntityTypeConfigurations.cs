using Application.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public sealed class MainMigrationJobEntityTypeConfigurations : IEntityTypeConfiguration<MainMigrationJob>
{
    public void Configure(EntityTypeBuilder<MainMigrationJob> builder)
    {
        builder.ToTable("graphql_main_migration_jobs");
        builder.HasKey(x => x.Job);
        builder.Property(x => x.Job)
            .HasColumnName("job");
        builder.Property(x => x.CreatedAt)
            .HasColumnName("created_at");
    }
}
