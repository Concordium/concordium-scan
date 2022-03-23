using Application.Api.GraphQL.EfCore.Converters;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Application.Api.GraphQL.EfCore.EntityTypeConfigurations;

public class ChainParametersConfiguration : IEntityTypeConfiguration<ChainParameters>
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
        builder.Property(x => x.BakerCooldownEpochs).HasColumnName("baker_cooldown_epochs");
        builder.Property(x => x.CredentialsPerBlockLimit).HasColumnName("credentials_per_block_limit");
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
        builder.Property(x => x.FoundationAccountId).HasColumnName("foundation_account_id");
        builder.Property(x => x.FoundationAccountAddress).HasColumnName("foundation_account_address").HasConversion<AccountAddressConverter>();
        builder.Property(x => x.MinimumThresholdForBaking).HasColumnName("minimum_threshold_for_baking");
    }
}