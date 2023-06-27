using Application.Api.GraphQL.EfCore.Converters.EfCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class ChainParametersConfiguration : IEntityTypeConfiguration<ChainParameters>,
    IEntityTypeConfiguration<ChainParametersV0>,
    IEntityTypeConfiguration<ChainParametersV1>
{
    public void Configure(EntityTypeBuilder<ChainParameters> builder)
    {
        builder.ToTable("graphql_chain_parameters");

        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).HasColumnName("id").ValueGeneratedOnAdd();
        builder.Property(x => x.ElectionDifficulty).HasColumnName("election_difficulty");
        builder.OwnsOne(cp => cp.EuroPerEnergy, euroPerEnergyBuilder =>
        {
            euroPerEnergyBuilder.Property(x => x.Numerator).HasColumnName("euro_per_energy_numerator");
            euroPerEnergyBuilder.Property(x => x.Denominator).HasColumnName("euro_per_energy_denominator");
        });
        builder.OwnsOne(cp => cp.MicroCcdPerEuro, microCcdPerEnergyBuilder =>
        {
            microCcdPerEnergyBuilder.Property(x => x.Numerator).HasColumnName("micro_ccd_per_euro_numerator");
            microCcdPerEnergyBuilder.Property(x => x.Denominator).HasColumnName("micro_ccd_per_euro_denominator");
        });
        builder.Property(x => x.AccountCreationLimit).HasColumnName("credentials_per_block_limit");
        builder.Property(x => x.FoundationAccountAddress).HasColumnName("foundation_account_address").HasConversion<AccountAddressConverter>();

        builder
            .HasDiscriminator<int>("version")
            .HasValue<ChainParametersV0>(0)
            .HasValue<ChainParametersV1>(1)
            .IsComplete();
    }

    public void Configure(EntityTypeBuilder<ChainParametersV0> builder)
    {
        builder.Property(x => x.BakerCooldownEpochs).HasColumnName("baker_cooldown_epochs");
        builder.OwnsOne(cp => cp.RewardParameters, rewardParametersBuilder =>
        {
            rewardParametersBuilder.OwnsOne(rp => rp.MintDistribution, mintDistributionBuilder =>
            {
                mintDistributionBuilder.Property(x => x.MintPerSlot).HasColumnName("mint_mint_per_slot");
                mintDistributionBuilder.Property(x => x.BakingReward).HasColumnName("mint_baking_reward");
                mintDistributionBuilder.Property(x => x.FinalizationReward).HasColumnName("mint_finalization_reward");
            });
            rewardParametersBuilder.OwnsOne(rp => rp.TransactionFeeDistribution, transactionFeeDistributionBuilder =>
            {
                transactionFeeDistributionBuilder.Property(x => x.Baker).HasColumnName("tx_fee_baker");
                transactionFeeDistributionBuilder.Property(x => x.GasAccount).HasColumnName("tx_fee_gas_account");
            });
            rewardParametersBuilder.OwnsOne(rp => rp.GasRewards, gasRewardsBuilder =>
            {
                gasRewardsBuilder.Property(x => x.Baker).HasColumnName("gas_baker");
                gasRewardsBuilder.Property(x => x.FinalizationProof).HasColumnName("gas_finalization_proof");
                gasRewardsBuilder.Property(x => x.AccountCreation).HasColumnName("gas_account_creation");
                gasRewardsBuilder.Property(x => x.ChainUpdate).HasColumnName("gas_chain_update");
            });
        });
        builder.Property(x => x.MinimumThresholdForBaking).HasColumnName("minimum_threshold_for_baking");
    }

    public void Configure(EntityTypeBuilder<ChainParametersV1> builder)
    {
        builder.Property(x => x.ElectionDifficulty).HasColumnName("election_difficulty");
        builder.OwnsOne(cp => cp.EuroPerEnergy, euroPerEnergyBuilder =>
        {
            euroPerEnergyBuilder.Property(x => x.Numerator).HasColumnName("euro_per_energy_numerator");
            euroPerEnergyBuilder.Property(x => x.Denominator).HasColumnName("euro_per_energy_denominator");
        });
        builder.OwnsOne(cp => cp.MicroCcdPerEuro, microCcdPerEnergyBuilder =>
        {
            microCcdPerEnergyBuilder.Property(x => x.Numerator).HasColumnName("micro_ccd_per_euro_numerator");
            microCcdPerEnergyBuilder.Property(x => x.Denominator).HasColumnName("micro_ccd_per_euro_denominator");
        });
        builder.Property(x => x.PoolOwnerCooldown).HasColumnName("pool_owner_cooldown");
        builder.Property(x => x.DelegatorCooldown).HasColumnName("delegator_cooldown");
        builder.Property(x => x.RewardPeriodLength).HasColumnName("reward_period_length");
        builder.Property(x => x.MintPerPayday).HasColumnName("mint_per_payday");
        builder.Property(x => x.AccountCreationLimit).HasColumnName("credentials_per_block_limit");
        builder.OwnsOne(cp => cp.RewardParameters, rewardParametersBuilder =>
        {
            rewardParametersBuilder.OwnsOne(rp => rp.MintDistribution, mintDistributionBuilder =>
            {
                mintDistributionBuilder.Property(x => x.BakingReward).HasColumnName("mint_baking_reward");
                mintDistributionBuilder.Property(x => x.FinalizationReward).HasColumnName("mint_finalization_reward");
            });
            rewardParametersBuilder.OwnsOne(rp => rp.TransactionFeeDistribution, transactionFeeDistributionBuilder =>
            {
                transactionFeeDistributionBuilder.Property(x => x.Baker).HasColumnName("tx_fee_baker");
                transactionFeeDistributionBuilder.Property(x => x.GasAccount).HasColumnName("tx_fee_gas_account");
            });
            rewardParametersBuilder.OwnsOne(rp => rp.GasRewards, gasRewardsBuilder =>
            {
                gasRewardsBuilder.Property(x => x.Baker).HasColumnName("gas_baker");
                gasRewardsBuilder.Property(x => x.FinalizationProof).HasColumnName("gas_finalization_proof");
                gasRewardsBuilder.Property(x => x.AccountCreation).HasColumnName("gas_account_creation");
                gasRewardsBuilder.Property(x => x.ChainUpdate).HasColumnName("gas_chain_update");
            });
        });
        builder.Property(x => x.FoundationAccountAddress).HasColumnName("foundation_account_address").HasConversion<AccountAddressConverter>();
        builder.Property(x => x.PassiveFinalizationCommission).HasColumnName("passive_finalization_commission");
        builder.Property(x => x.PassiveBakingCommission).HasColumnName("passive_baking_commission");
        builder.Property(x => x.PassiveTransactionCommission).HasColumnName("passive_transaction_commission");
        builder.OwnsOne(cp => cp.FinalizationCommissionRange, rangeBuilder =>
        {
            rangeBuilder.Property(x => x.Min).HasColumnName("finalization_commission_range_min");
            rangeBuilder.Property(x => x.Max).HasColumnName("finalization_commission_range_max");
        });
        builder.OwnsOne(cp => cp.BakingCommissionRange, rangeBuilder =>
        {
            rangeBuilder.Property(x => x.Min).HasColumnName("baking_commission_range_min");
            rangeBuilder.Property(x => x.Max).HasColumnName("baking_commission_range_max");
        });
        builder.OwnsOne(cp => cp.TransactionCommissionRange, rangeBuilder =>
        {
            rangeBuilder.Property(x => x.Min).HasColumnName("transaction_commission_range_min");
            rangeBuilder.Property(x => x.Max).HasColumnName("transaction_commission_range_max");
        });
        builder.Property(x => x.MinimumEquityCapital).HasColumnName("minimum_equity_capital");
        builder.Property(x => x.CapitalBound).HasColumnName("capital_bound");
        builder.OwnsOne(cp => cp.LeverageBound, leverageBuilder =>
        {
            leverageBuilder.Property(x => x.Numerator).HasColumnName("leverage_bound_numerator");
            leverageBuilder.Property(x => x.Denominator).HasColumnName("leverage_bound_denominator");
        });
    }
}