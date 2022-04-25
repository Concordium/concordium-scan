using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class SpecialEventConfiguration : 
    IEntityTypeConfiguration<SpecialEvent>,
    IEntityTypeConfiguration<PaydayAccountRewardSpecialEvent>,
    IEntityTypeConfiguration<BlockAccrueRewardSpecialEvent>,
    IEntityTypeConfiguration<PaydayFoundationRewardSpecialEvent>,
    IEntityTypeConfiguration<PaydayPoolRewardSpecialEvent>
{
    public void Configure(EntityTypeBuilder<SpecialEvent> builder)
    {
        builder.ToTable("graphql_special_events");

        builder.HasKey(x => new { x.BlockId, x.Index });
        builder.Property(x => x.BlockId).HasColumnName("block_id").ValueGeneratedNever();
        builder.Property(x => x.Index).HasColumnName("index").ValueGeneratedOnAdd();

        builder
            .HasDiscriminator<int>("type_id")
            .HasValue<PaydayAccountRewardSpecialEvent>(1)
            .HasValue<BlockAccrueRewardSpecialEvent>(2)
            .HasValue<PaydayFoundationRewardSpecialEvent>(3)
            .HasValue<PaydayPoolRewardSpecialEvent>(4)
            .IsComplete();
    }

    public void Configure(EntityTypeBuilder<PaydayAccountRewardSpecialEvent> builder)
    {
        builder.Property(x => x.Account).HasColumnName("account_address").HasConversion<AccountAddressConverter>();
        builder.Property(x => x.TransactionFees).HasColumnName("transaction_fees");
        builder.Property(x => x.BakerReward).HasColumnName("baker_reward");
        builder.Property(x => x.FinalizationReward).HasColumnName("finalization_reward");
    }

    public void Configure(EntityTypeBuilder<BlockAccrueRewardSpecialEvent> builder)
    {
        builder.Property(x => x.TransactionFees).HasColumnName("transaction_fees");
        builder.Property(x => x.OldGasAccount).HasColumnName("old_gas_account");
        builder.Property(x => x.NewGasAccount).HasColumnName("new_gas_account");
        builder.Property(x => x.BakerReward).HasColumnName("baker_reward");
        builder.Property(x => x.LPoolReward).HasColumnName("l_pool_reward");
        builder.Property(x => x.FoundationCharge).HasColumnName("foundation_charge");
        builder.Property(x => x.BakerId).HasColumnName("baker_id");
    }

    public void Configure(EntityTypeBuilder<PaydayFoundationRewardSpecialEvent> builder)
    {
        builder.Property(x => x.FoundationAccount).HasColumnName("account_address").HasConversion<AccountAddressConverter>();
        builder.Property(x => x.DevelopmentCharge).HasColumnName("foundation_charge");
    }

    public void Configure(EntityTypeBuilder<PaydayPoolRewardSpecialEvent> builder)
    {
        builder.Property(x => x.PoolOwner).HasColumnName("pool_owner");
        builder.Property(x => x.TransactionFees).HasColumnName("transaction_fees");
        builder.Property(x => x.BakerReward).HasColumnName("baker_reward");
        builder.Property(x => x.FinalizationReward).HasColumnName("finalization_reward");
    }
}