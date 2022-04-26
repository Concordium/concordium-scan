using Application.Api.GraphQL.Accounts;
using Application.Api.GraphQL.Blocks;
using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class SpecialEventConfiguration : 
    IEntityTypeConfiguration<SpecialEvent>,
    IEntityTypeConfiguration<MintSpecialEvent>,
    IEntityTypeConfiguration<FinalizationRewardsSpecialEvent>,
    IEntityTypeConfiguration<BlockRewardsSpecialEvent>,
    IEntityTypeConfiguration<BakingRewardsSpecialEvent>,
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
            .HasValue<MintSpecialEvent>(1)
            .HasValue<FinalizationRewardsSpecialEvent>(2)
            .HasValue<BlockRewardsSpecialEvent>(3)
            .HasValue<BakingRewardsSpecialEvent>(4)
            .HasValue<PaydayAccountRewardSpecialEvent>(5)
            .HasValue<BlockAccrueRewardSpecialEvent>(6)
            .HasValue<PaydayFoundationRewardSpecialEvent>(7)
            .HasValue<PaydayPoolRewardSpecialEvent>(8)
            .IsComplete();
    }

    public void Configure(EntityTypeBuilder<MintSpecialEvent> builder)
    {
        builder.Property(x => x.BakingReward).HasColumnName("baker_reward");
        builder.Property(x => x.FinalizationReward).HasColumnName("finalization_reward");
        builder.Property(x => x.PlatformDevelopmentCharge).HasColumnName("foundation_charge");
        builder.Property(x => x.FoundationAccountAddress).HasColumnName("foundation_account_address").HasConversion<AccountAddressConverter>();
    }

    public void Configure(EntityTypeBuilder<FinalizationRewardsSpecialEvent> builder)
    {
        builder.Property(x => x.Remainder).HasColumnName("remainder");
        builder.Property(x => x.AccountAddresses).HasColumnName("account_addresses").HasPostgresArrayConversion<AccountAddress, string>(new AccountAddressConverter());
        builder.Property(x => x.Amounts).HasColumnName("amounts");
    }

    public void Configure(EntityTypeBuilder<BlockRewardsSpecialEvent> builder)
    {
        builder.Property(x => x.TransactionFees).HasColumnName("transaction_fees");
        builder.Property(x => x.OldGasAccount).HasColumnName("old_gas_account");
        builder.Property(x => x.NewGasAccount).HasColumnName("new_gas_account");
        builder.Property(x => x.BakerReward).HasColumnName("baker_reward");
        builder.Property(x => x.FoundationCharge).HasColumnName("foundation_charge");
        builder.Property(x => x.BakerAccountAddress).HasColumnName("account_address").HasConversion<AccountAddressConverter>();
        builder.Property(x => x.FoundationAccountAddress).HasColumnName("foundation_account_address").HasConversion<AccountAddressConverter>();
    }

    public void Configure(EntityTypeBuilder<BakingRewardsSpecialEvent> builder)
    {
        builder.Property(x => x.Remainder).HasColumnName("remainder");
        builder.Property(x => x.AccountAddresses).HasColumnName("account_addresses").HasPostgresArrayConversion<AccountAddress, string>(new AccountAddressConverter());
        builder.Property(x => x.Amounts).HasColumnName("amounts");
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
        builder.Property(x => x.FoundationAccount).HasColumnName("foundation_account_address").HasConversion<AccountAddressConverter>();
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