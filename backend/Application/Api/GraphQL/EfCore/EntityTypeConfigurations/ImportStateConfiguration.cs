using Application.Api.GraphQL.EfCore.Converters;
using Application.Api.GraphQL.Import;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class ImportStateConfiguration : IEntityTypeConfiguration<ImportState>
{
    public void Configure(EntityTypeBuilder<ImportState> builder)
    {
        builder.ToTable("graphql_import_state");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.GenesisBlockHash).HasColumnName("genesis_block_hash");
        builder.Property(x => x.MaxImportedBlockHeight).HasColumnName("max_imported_block_height");
        builder.Property(x => x.CumulativeAccountsCreated).HasColumnName("cumulative_accounts_created");
        builder.Property(x => x.CumulativeTransactionCount).HasColumnName("cumulative_transaction_count");
        builder.Property(x => x.LastBlockSlotTime).HasColumnName("last_block_slot_time").HasConversion<DateTimeOffsetToTimestampConverter>();
        builder.Property(x => x.MaxBlockHeightWithUpdatedFinalizationTime).HasColumnName("max_block_height_with_updated_finalization_time");
        builder.Ignore(x => x.LatestWrittenChainParameters);
    }
}